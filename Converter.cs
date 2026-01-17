using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Markdig;
using PuppeteerSharp;
using PuppeteerSharp.Media;


namespace MdToPdf;

partial class Converter
{
    private readonly MarkdownPipeline _pipeline;

    public Converter()
    {
        _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();
    }

    public async Task ConvertFile(string filePath, string outputDir)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The specified file does not exist.", filePath);
        }

        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string pdfPath = Path.Combine(outputDir, $"{fileName}.pdf");

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        string markdownContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        string mdDir = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? "";

        // 1. Pre-process Obsidian image wikilinks: ![[image.png]], ![[image.png|300]]
        markdownContent = WikilinkRegex().Replace(markdownContent, m =>
        {
            var content = m.Groups[1].Value;
            var parts = content.Split('|');
            var imageName = Uri.UnescapeDataString(parts[0].Trim());

            // Try to find the image: first in same folder, then search recursively
            string? fullPath = FindImageFile(imageName, mdDir);
            if (fullPath == null)
                return m.Value;

            string? width = null;
            string alt = "";
            for (int i = 1; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i].Trim(), out _)) width = parts[i].Trim();
                else alt = parts[i].Trim();
            }

            // Convert to Base64 Data URI
            string? dataUri = ConvertImageToDataUri(fullPath);
            if (dataUri != null)
            {
                string widthAttr = width != null ? $" width=\"{width}\"" : "";
                return $"<img src=\"{dataUri}\" alt=\"{WebUtility.HtmlEncode(alt)}\"{widthAttr} />";
            }
            return m.Value;
        });

        // 2. Pre-process standard Markdown images: ![alt](image.png)
        markdownContent = MarkdownImageRegex().Replace(markdownContent, m =>
        {
            var alt = m.Groups[1].Value;
            var imageName = Uri.UnescapeDataString(m.Groups[2].Value.Trim());

            if (!imageName.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                string? fullPath = FindImageFile(imageName, mdDir);
                if (fullPath != null)
                {
                    string? dataUri = ConvertImageToDataUri(fullPath);
                    if (dataUri != null)
                    {
                        return $"<img src=\"{dataUri}\" alt=\"{WebUtility.HtmlEncode(alt)}\" />";
                    }
                }
            }
            return m.Value;
        });

        string htmlBody = Markdown.ToHtml(markdownContent, _pipeline);

        if (!htmlBody.Contains("<h1"))
        {
            htmlBody = $"<h1>{fileName}</h1>\n{htmlBody}";
        }

        string finalHtml = BuildHtmlWrapper(htmlBody);

        await GeneratePdf(finalHtml, pdfPath);
    }

    private string BuildHtmlWrapper(string htmlBody)
    {
        return $@"
                    <!DOCTYPE html>
                    <html lang='es'>
                    <head>
                        <meta charset='UTF-8'>
                        <style>
                            body {{ font-family: 'Segoe UI', Arial, sans-serif; padding: 40px; line-height: 1.6; color: #333; }}
                            img {{ max-width: 100%; height: auto; display: block; margin: 20px auto; }}
                            h1 {{ color: #ADD8E6; text-align: center; margin-bottom: 30px; }}
                            pre {{ background: #f4f4f4; padding: 15px; border-radius: 5px; overflow-x: auto; }}
                            p {{ margin-bottom: 15px; }}
                        </style>
                    </head>
                    <body>{htmlBody}</body>
                    </html>";
    }

    private async Task GeneratePdf(string html, string destination)
    {
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();

        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
        });

        await using var page = await browser.NewPageAsync();

        await page.SetContentAsync(html, new NavigationOptions
        {
            WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
        });

        await page.PdfAsync(destination, new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            MarginOptions = new MarginOptions
            {
                Top = "20px",
                Bottom = "20px",
                Left = "20px",
                Right = "20px"
            }

        });
    }

    [GeneratedRegex(@"!\[\[(.*?)\]\]")]
    private static partial Regex WikilinkRegex();

    [GeneratedRegex(@"!\[(.*?)\]\((.*?)\)")]
    private static partial Regex MarkdownImageRegex();

    private static string? ConvertImageToDataUri(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
                return null;

            byte[] imageBytes = File.ReadAllBytes(imagePath);
            string base64 = Convert.ToBase64String(imageBytes);

            // Determine MIME type based on extension
            string extension = Path.GetExtension(imagePath).ToLowerInvariant();
            string mimeType = extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".bmp" => "image/bmp",
                _ => "image/png" // Default fallback
            };

            return $"data:{mimeType};base64,{base64}";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Finds an image file by name. First checks if it's a direct path,
    /// then searches in the same directory, then searches recursively
    /// upward to find the Obsidian vault root and searches the entire vault.
    /// </summary>
    private static string? FindImageFile(string imageName, string startDir)
    {
        // If it's already a full path and exists, use it
        if (Path.IsPathRooted(imageName) && File.Exists(imageName))
            return imageName;

        // Check in the same directory as the markdown file
        string directPath = Path.Combine(startDir, imageName);
        if (File.Exists(directPath))
            return directPath;

        // Find the Obsidian vault root (look for .obsidian folder)
        string? vaultRoot = FindVaultRoot(startDir);
        if (vaultRoot != null)
        {
            // Search recursively in the entire vault
            try
            {
                var files = Directory.GetFiles(vaultRoot, Path.GetFileName(imageName), SearchOption.AllDirectories);
                if (files.Length > 0)
                    return files[0];
            }
            catch { }
        }

        // Fallback: search in parent directories up to 5 levels
        string? currentDir = startDir;
        for (int i = 0; i < 5 && currentDir != null; i++)
        {
            try
            {
                var files = Directory.GetFiles(currentDir, Path.GetFileName(imageName), SearchOption.AllDirectories);
                if (files.Length > 0)
                    return files[0];
            }
            catch { }

            currentDir = Path.GetDirectoryName(currentDir);
        }

        return null;
    }

    /// <summary>
    /// Finds the Obsidian vault root by looking for the .obsidian folder
    /// </summary>
    private static string? FindVaultRoot(string startDir)
    {
        string? currentDir = startDir;
        while (currentDir != null)
        {
            if (Directory.Exists(Path.Combine(currentDir, ".obsidian")))
                return currentDir;
            currentDir = Path.GetDirectoryName(currentDir);
        }
        return null;
    }
}
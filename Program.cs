namespace MdToPdf;

class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            Helper();
            return;
        }

        try
        {

            string command = args[0];

            switch (command)
            {
                case "--file":
                case "-f":
                    await SingleFile(args);
                    break;
                case "--all":
                case "-a":
                    await AllDirectories(args);
                    break;
                case "--help":
                case "-h":
                    Helper();
                    break;
                default:
                    Console.WriteLine("✘ Invalid command.");
                    Helper();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        static async Task SingleFile(string[] args)
        {
            var converter = new Converter();

            if (args.Length < 2) throw new ArgumentException("Missing file path.");
            string filePath = args[1];
            if (!File.Exists(filePath)) throw new FileNotFoundException("The specified file does not exist.", filePath);

            string outputDir = Path.Combine(Path.GetDirectoryName(filePath)!, "ExportPDF");

            Console.WriteLine($"✔ Processed individual file: {filePath}");
            await converter.ConvertFile(filePath, outputDir);
        }

        static async Task AllDirectories(string[] args)
        {
            var converter = new Converter();

            string directoryPath = Path.GetFullPath(args.Length < 2 ? Directory.GetCurrentDirectory() : args[1]);

            string outputDir = Path.Combine(directoryPath, "ExportPDF");

            var files = Directory.GetFiles(directoryPath, "*.md", SearchOption.AllDirectories);

            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            await Parallel.ForEachAsync(files, options, async (file, cancellationToken) =>
            {
                try
                {
                    await converter.ConvertFile(file, outputDir);
                    Console.WriteLine($"✔ Processed file: {file}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✘ Error processing file {file}: {ex.Message}");
                }
            });
        }
        static void Helper()
        {
            Console.WriteLine("Usage: mdtopdf [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --file, -f <file>    Convert a single Markdown file to PDF.");
            Console.WriteLine("  --all, -a <directory> Convert all Markdown files in a directory to PDF.");
            Console.WriteLine("  --help, -h            Show this help message.");
        }
    }
}


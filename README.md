# MdToPdf

**MdToPdf** es una herramienta de línea de comandos potente y sencilla diseñada para convertir archivos Markdown a PDF con una estética premium, optimizada específicamente para usuarios de **Obsidian**.

## ✨ Características

- 🚀 **Conversión Ultra-Rápida:** Convierte archivos individuales o directorios completos.
- 🎨 **Estética Premium:** Genera PDFs con una tipografía moderna (`Segoe UI`), títulos centrados y un diseño limpio.
- 🔗 **Soporte de Obsidian:**
  - Procesa **Wikilinks** de imágenes: `![[imagen.png|300]]`.
  - **Búsqueda Inteligente de Imágenes:** Encuentra automáticamente tus imágenes dentro de tu Vault de Obsidian, incluso si están en carpetas separadas (como `attachments`).
  - **Incrustación Total:** Las imágenes se incrustan como Base64, lo que garantiza que el PDF sea totalmente portátil y no dependa de archivos externos.
- 🛠️ **Basado en Puppeteer:** Utiliza el motor de Chromium para un renderizado fiel al estilo web.

## 🚀 Uso

Ejecuta la aplicación desde la consola de comandos:

### Convertir un solo archivo
```powershell
dotnet run -- -f "C:\ruta\a\tu\archivo.md"
```
*Esto creará una carpeta `ExportPDF` en la misma ubicación del archivo con el resultado.*

### Convertir una carpeta completa (recursivo)
```powershell
dotnet run -- -a "C:\ruta\a\tu\vault"
```
*Busca todos los archivos `.md` en la carpeta y subcarpetas, exportándolos a `ExportPDF`.*

### Ayuda
```powershell
dotnet run -- --help
```

## 🛠️ Tecnologías

- **.NET 10.0**
- **Markdig:** Para el procesamiento de Markdown.
- **PuppeteerSharp:** Para la generación de archivos PDF de alta calidad.
- **Source Generators:** Utiliza `GeneratedRegex` para un rendimiento óptimo en el procesamiento de patrones.

## 📦 Instalación y Desarrollo

1. Asegúrate de tener instalado el SDK de **.NET 10**.
2. Clona el repositorio.
3. Ejecuta `dotnet build` para restaurar dependencias y compilar.

---
Creado para transformar tus notas de Obsidian en documentos profesionales.

using ERPSystem.DocumentEngine.Models;

namespace ERPSystem.DocumentEngine.Renderer;

/// <summary>
/// Pluggable HTML-to-PDF converter. The engine itself stays dependency-free:
/// the actual conversion (headless Chromium, wkhtmltopdf, PuppeteerSharp, a
/// print API, etc.) is provided by the host application and injected here.
/// The engine always feeds it the SAME HTML it produces for web/print.
/// </summary>
public interface IPdfConverter
{
    byte[] Convert(string html, RenderOptions options);
}

using ERPSystem.DocumentEngine.Models;

namespace ERPSystem.DocumentEngine.Renderer;

/// <summary>
/// Produces PDF output from the SAME HTML template. The engine generates
/// print-ready, self-contained HTML and delegates the byte conversion to an
/// injected <see cref="IPdfConverter"/>. If no converter is supplied,
/// <see cref="RenderHtml"/> still yields the exact HTML a PDF engine should
/// consume, so downstream tooling can convert it however it likes.
/// </summary>
public sealed class PdfRenderer
{
    private readonly HtmlRenderer _html;
    private readonly IPdfConverter? _converter;

    public PdfRenderer(HtmlRenderer html, IPdfConverter? converter = null)
    {
        _html = html;
        _converter = converter;
    }

    public PdfRenderer() : this(new HtmlRenderer()) { }

    public bool CanProducePdfBytes => _converter is not null;

    /// <summary>The PDF-ready HTML (identical structure to print output).</summary>
    public string RenderHtml(DocumentModel model, RenderOptions? options = null)
    {
        options = CloneForPdf(options);
        return _html.Render(model, options);
    }

    /// <summary>Renders to PDF bytes using the injected converter.</summary>
    public byte[] RenderPdf(DocumentModel model, RenderOptions? options = null)
    {
        var opts = CloneForPdf(options);
        var html = _html.Render(model, opts);

        if (_converter is null)
        {
            throw new InvalidOperationException(
                "No IPdfConverter was registered. Inject a host-provided converter " +
                "(e.g. headless Chromium / PuppeteerSharp / wkhtmltopdf) to produce PDF bytes, " +
                "or call RenderHtml() and convert the HTML yourself.");
        }

        return _converter.Convert(html, opts);
    }

    private static RenderOptions CloneForPdf(RenderOptions? source)
    {
        var o = source ?? new RenderOptions();
        o.Mode = RenderMode.Pdf;
        o.IncludePreviewToolbar = false;
        o.InlineStyles = true;
        return o;
    }
}

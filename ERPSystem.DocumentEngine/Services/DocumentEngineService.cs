using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Renderer;

namespace ERPSystem.DocumentEngine.Services;

/// <summary>
/// The single public entry point of the Document Engine. Exposes the three
/// required render services — <see cref="RenderHtml"/>, <see cref="RenderPrint"/>
/// and <see cref="RenderPdf"/> — all backed by the SAME templates and HTML.
///
/// Usage (host-agnostic):
/// <code>
/// var engine = DocumentEngineService.Create();
/// string html = engine.RenderHtml(model, options);
/// </code>
/// </summary>
public sealed class DocumentEngineService
{
    private readonly HtmlRenderer _htmlRenderer;
    private readonly PrintRenderer _printRenderer;
    private readonly PdfRenderer _pdfRenderer;

    public DocumentEngineService(
        HtmlRenderer htmlRenderer,
        PrintRenderer printRenderer,
        PdfRenderer pdfRenderer)
    {
        _htmlRenderer = htmlRenderer;
        _printRenderer = printRenderer;
        _pdfRenderer = pdfRenderer;
    }

    /// <summary>Builds a fully-wired engine. Optionally supply a PDF converter.</summary>
    public static DocumentEngineService Create(IPdfConverter? pdfConverter = null)
    {
        var assets = new AssetProvider();
        var registry = new TemplateRegistry();
        var shell = new DocumentShell(assets);
        var html = new HtmlRenderer(registry, shell);
        return new DocumentEngineService(
            html,
            new PrintRenderer(html),
            new PdfRenderer(html, pdfConverter));
    }

    /// <summary>Renders web / preview HTML (responsive, self-contained by default).</summary>
    public string RenderHtml(DocumentModel model, RenderOptions? options = null) =>
        _htmlRenderer.Render(model, options);

    /// <summary>Renders print-ready HTML (A4, page breaks, repeated headers).</summary>
    public string RenderPrint(DocumentModel model, RenderOptions? options = null) =>
        _printRenderer.Render(model, options);

    /// <summary>Renders the PDF-ready HTML (identical to print output).</summary>
    public string RenderPdf(DocumentModel model, RenderOptions? options = null) =>
        _pdfRenderer.RenderHtml(model, options);

    /// <summary>Renders PDF bytes (requires an injected <see cref="IPdfConverter"/>).</summary>
    public byte[] RenderPdfBytes(DocumentModel model, RenderOptions? options = null) =>
        _pdfRenderer.RenderPdf(model, options);

    /// <summary>Whether PDF byte output is available (a converter was injected).</summary>
    public bool CanProducePdfBytes => _pdfRenderer.CanProducePdfBytes;
}

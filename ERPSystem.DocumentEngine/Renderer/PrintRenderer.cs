using ERPSystem.DocumentEngine.Models;

namespace ERPSystem.DocumentEngine.Renderer;

/// <summary>
/// Produces print-ready HTML. It uses the SAME template and HTML as the web
/// renderer; only the render mode changes so print.css takes over (A4, page
/// breaks, repeated headers, hidden UI chrome). No separate layout exists.
/// </summary>
public sealed class PrintRenderer
{
    private readonly HtmlRenderer _html;

    public PrintRenderer(HtmlRenderer html) => _html = html;

    public PrintRenderer() : this(new HtmlRenderer()) { }

    public string Render(DocumentModel model, RenderOptions? options = null)
    {
        options = CloneForPrint(options);
        return _html.Render(model, options);
    }

    private static RenderOptions CloneForPrint(RenderOptions? source)
    {
        var o = source ?? new RenderOptions();
        o.Mode = RenderMode.Print;
        o.IncludePreviewToolbar = false;
        o.InlineStyles = true; // print/pdf must be self-contained
        return o;
    }
}

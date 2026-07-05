using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Services;

namespace ERPSystem.DocumentEngine.Renderer;

/// <summary>
/// The core renderer. Turns a <see cref="DocumentModel"/> into a complete,
/// self-contained, responsive HTML document. Print and PDF renderers reuse this
/// exact output — HTML is the single source of truth.
/// </summary>
public sealed class HtmlRenderer
{
    private readonly TemplateRegistry _registry;
    private readonly DocumentShell _shell;

    public HtmlRenderer(TemplateRegistry registry, DocumentShell shell)
    {
        _registry = registry;
        _shell = shell;
    }

    public HtmlRenderer()
        : this(new TemplateRegistry(), new DocumentShell(new AssetProvider()))
    {
    }

    /// <summary>Renders the document to a full HTML string.</summary>
    public string Render(DocumentModel model, RenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        options ??= new RenderOptions();

        var ctx = new RenderContext(options);
        var template = _registry.Resolve(model.Type);
        var body = template.RenderBody(model, ctx);
        return _shell.Wrap(model, ctx, body);
    }
}

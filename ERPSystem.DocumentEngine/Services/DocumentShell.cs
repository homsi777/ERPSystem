using System.Text;
using ERPSystem.DocumentEngine.Components;
using ERPSystem.DocumentEngine.Helpers;
using ERPSystem.DocumentEngine.Models;

namespace ERPSystem.DocumentEngine.Services;

/// <summary>
/// Wraps a template's body content into a complete, responsive HTML document:
/// &lt;html&gt; direction/theme, &lt;head&gt; styles, the A4 page shell, the
/// shared company header/footer, and the watermark. This is the ONLY place a
/// full page is assembled, so every document shares one structure regardless of
/// render mode (web / print / pdf).
/// </summary>
public sealed class DocumentShell
{
    private readonly AssetProvider _assets;

    public DocumentShell(AssetProvider assets) => _assets = assets;

    public string Wrap(DocumentModel model, RenderContext ctx, string bodyHtml)
    {
        var o = ctx.Options;
        var dir = Html.DirValue(ctx.Direction);
        var lang = string.IsNullOrWhiteSpace(o.Language) ? (ctx.IsRtl ? "ar" : "en") : o.Language;
        var docClass = ctx.IsRtl ? "doc-rtl" : "doc-ltr";
        var title = string.IsNullOrWhiteSpace(model.Number)
            ? model.Title
            : $"{model.Title} — {model.Number}";

        var sb = new StringBuilder(80 * 1024);
        sb.Append("<!DOCTYPE html>\n");
        sb.Append($"<html lang=\"{Html.Attr(lang)}\" dir=\"{dir}\" data-theme=\"{Html.Attr(o.Theme)}\">\n");

        // Head
        sb.Append("<head>\n");
        sb.Append("<meta charset=\"utf-8\">\n");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        sb.Append($"<meta name=\"generator\" content=\"ERPSystem.DocumentEngine\">\n");
        sb.Append($"<title>{Html.Escape(title)}</title>\n");
        sb.Append(StyleTag(o));
        sb.Append("</head>\n");

        // Body
        sb.Append($"<body class=\"{ctx.ModeBodyClass} {docClass}\">\n");

        if (o.IncludePreviewToolbar && o.Mode == RenderMode.Web)
        {
            sb.Append(PreviewToolbar(ctx));
        }

        sb.Append("<div class=\"doc-canvas\">\n");
        sb.Append("<article class=\"doc-page\">\n");
        sb.Append(Components.Components.Watermark(ctx, model));
        sb.Append("<div class=\"doc-page__inner\">\n");

        sb.Append("<div class=\"doc-header\">");
        sb.Append(Components.Components.CompanyHeader(ctx, model));
        sb.Append("</div>\n");

        sb.Append("<div class=\"doc-body\">\n");
        sb.Append(bodyHtml);
        sb.Append("\n</div>\n");

        sb.Append(Components.Components.CompanyFooter(ctx));

        sb.Append("</div>\n");   // doc-page__inner
        sb.Append("</article>\n");
        sb.Append("</div>\n");   // doc-canvas

        sb.Append("</body>\n");
        sb.Append("</html>\n");
        return sb.ToString();
    }

    private string StyleTag(RenderOptions o)
    {
        if (!o.InlineStyles && !string.IsNullOrWhiteSpace(o.StylesheetHref))
        {
            return $"<link rel=\"stylesheet\" href=\"{Html.Attr(o.StylesheetHref)}\">\n";
        }

        return "<style>\n" + _assets.GetCombinedStyles() + "\n</style>\n";
    }

    private static string PreviewToolbar(RenderContext ctx)
    {
        var lbl = ctx.Labels;
        return "<div class=\"ui-toolbar no-print\">" +
               $"<button type=\"button\" class=\"ui-btn ui-btn--primary\" onclick=\"window.print()\">{Html.Escape(lbl.PreviewPrint)}</button>" +
               $"<button type=\"button\" class=\"ui-btn\" onclick=\"window.print()\">{Html.Escape(lbl.PreviewDownload)}</button>" +
               "</div>\n";
    }
}

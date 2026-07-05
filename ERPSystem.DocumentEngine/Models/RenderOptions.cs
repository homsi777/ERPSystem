namespace ERPSystem.DocumentEngine.Models;

/// <summary>
/// Controls how a document model is turned into HTML. These options never
/// change the visual identity — only mode, direction, theme and packaging.
/// </summary>
public sealed class RenderOptions
{
    public RenderMode Mode { get; set; } = RenderMode.Web;
    public TextDirection Direction { get; set; } = TextDirection.Rtl;

    /// <summary>Theme variable set: "light", "dark", "company", ...</summary>
    public string Theme { get; set; } = "light";

    /// <summary>UI language for engine chrome (e.g. "ar", "en").</summary>
    public string Language { get; set; } = "ar";

    /// <summary>
    /// When true the CSS design system is inlined into the document, producing
    /// a fully self-contained portable .html file (ideal for PDF / email /
    /// archive). When false a &lt;link&gt; to <see cref="StylesheetHref"/> is
    /// emitted instead (ideal for web hosting with shared cached CSS).
    /// </summary>
    public bool InlineStyles { get; set; } = true;

    /// <summary>External stylesheet href used when <see cref="InlineStyles"/> is false.</summary>
    public string? StylesheetHref { get; set; }

    /// <summary>Emit an on-screen preview toolbar (hidden automatically on print).</summary>
    public bool IncludePreviewToolbar { get; set; }

    public PageSize PageSize { get; set; } = PageSize.A4;

    /// <summary>Company identity used to fill all branding placeholders.</summary>
    public CompanyBranding Branding { get; set; } = new();

    /// <summary>Localised engine chrome strings. Auto-derived when left null.</summary>
    public DocumentLabels? Labels { get; set; }

    public DocumentLabels ResolveLabels() => Labels ?? DocumentLabels.For(Language, Direction);

    public static RenderOptions Web() => new() { Mode = RenderMode.Web, IncludePreviewToolbar = true };
    public static RenderOptions Print() => new() { Mode = RenderMode.Print };
    public static RenderOptions Pdf() => new() { Mode = RenderMode.Pdf };
}

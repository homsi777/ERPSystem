using ERPSystem.DocumentEngine.Models;

namespace ERPSystem.DocumentEngine.Services;

/// <summary>
/// Immutable per-render context handed to every component and template. Bundles
/// the resolved options, labels and branding so components never reach for
/// global state and remain pure string producers.
/// </summary>
public sealed class RenderContext
{
    public RenderContext(RenderOptions options)
    {
        Options = options ?? new RenderOptions();
        Labels = Options.ResolveLabels();
        Branding = Options.Branding;
    }

    public RenderOptions Options { get; }
    public DocumentLabels Labels { get; }
    public CompanyBranding Branding { get; }

    public TextDirection Direction => Options.Direction;
    public RenderMode Mode => Options.Mode;
    public bool IsRtl => Options.Direction == TextDirection.Rtl;

    public string ModeBodyClass => Mode switch
    {
        RenderMode.Print => "mode-print",
        RenderMode.Pdf => "mode-pdf",
        _ => "mode-web"
    };
}

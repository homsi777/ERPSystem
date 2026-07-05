using System.Text;
using ERPSystem.DocumentEngine.Models;

namespace ERPSystem.DocumentEngine.Helpers;

/// <summary>
/// Minimal, dependency-free HTML helpers: safe escaping, attribute emission,
/// and mapping of engine enums to design-system CSS classes. Keeping this in
/// one place guarantees consistent, injection-safe markup across every
/// component and template.
/// </summary>
public static class Html
{
    /// <summary>HTML-encodes text for element content.</summary>
    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value!.Length + 16);
        foreach (var c in value)
        {
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                case '\'': sb.Append("&#39;"); break;
                default: sb.Append(c); break;
            }
        }

        return sb.ToString();
    }

    /// <summary>Escapes and converts newlines to &lt;br&gt; for multi-line text.</summary>
    public static string EscapeMultiline(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return Escape(value)
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\n", "<br>");
    }

    /// <summary>Escapes a value for use inside a double-quoted attribute.</summary>
    public static string Attr(string? value) => Escape(value);

    /// <summary>Renders an attribute (name="value") or empty string when no value.</summary>
    public static string OptAttr(string name, string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : $" {name}=\"{Attr(value)}\"";

    public static string AlignClass(TextAlign align) => align switch
    {
        TextAlign.Center => "text-center",
        TextAlign.End => "text-end",
        _ => "text-start"
    };

    public static string AccentToken(Accent accent) => accent switch
    {
        Accent.Primary => "primary",
        Accent.Secondary => "secondary",
        Accent.Success => "success",
        Accent.Danger => "danger",
        Accent.Warning => "warning",
        Accent.Info => "info",
        _ => "neutral"
    };

    public static string BadgeClass(Accent accent) => $"badge badge--{AccentToken(accent)}";

    public static string MetricAccentClass(Accent accent) => accent switch
    {
        Accent.Success => "metric--success",
        Accent.Danger => "metric--danger",
        Accent.Warning => "metric--warning",
        Accent.Info => "metric--info",
        _ => "metric--primary"
    };

    public static string TimelineAccentClass(Accent accent) => accent switch
    {
        Accent.Success => "timeline__item--success",
        Accent.Danger => "timeline__item--danger",
        Accent.Warning => "timeline__item--warning",
        Accent.Neutral => "timeline__item--muted",
        _ => string.Empty
    };

    public static string PartyClass(PartyKind kind) => kind switch
    {
        PartyKind.Supplier => "party-card party-card--supplier",
        PartyKind.Partner => "party-card party-card--partner",
        _ => "party-card"
    };

    /// <summary>Maps a document status to a badge accent + fallback label.</summary>
    public static (Accent Accent, string Label) StatusMeta(DocumentStatus status) => status switch
    {
        DocumentStatus.Draft => (Accent.Neutral, "Draft"),
        DocumentStatus.Pending => (Accent.Warning, "Pending"),
        DocumentStatus.Approved => (Accent.Info, "Approved"),
        DocumentStatus.Posted => (Accent.Info, "Posted"),
        DocumentStatus.Paid => (Accent.Success, "Paid"),
        DocumentStatus.PartiallyPaid => (Accent.Warning, "Partially Paid"),
        DocumentStatus.Overdue => (Accent.Danger, "Overdue"),
        DocumentStatus.Cancelled => (Accent.Danger, "Cancelled"),
        DocumentStatus.Rejected => (Accent.Danger, "Rejected"),
        DocumentStatus.Completed => (Accent.Success, "Completed"),
        _ => (Accent.Neutral, string.Empty)
    };

    public static string DirValue(TextDirection direction) => direction == TextDirection.Rtl ? "rtl" : "ltr";
}

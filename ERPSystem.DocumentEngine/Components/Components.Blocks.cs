using System.Text;
using ERPSystem.DocumentEngine.Helpers;
using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Services;

namespace ERPSystem.DocumentEngine.Components;

public static partial class Components
{
    /// <summary>Wraps content in a titled section band.</summary>
    public static string Section(string? title, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append("<section class=\"doc-section\">");
        if (!string.IsNullOrWhiteSpace(title))
        {
            sb.Append($"<div class=\"doc-section__title\">{Html.Escape(title)}</div>");
        }

        sb.Append(content);
        sb.Append("</section>");
        return sb.ToString();
    }

    public static string NotesBlock(RenderContext ctx, string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return string.Empty;
        }

        return "<div class=\"notes-block\">" +
               $"<div class=\"notes-block__title\">{Html.Escape(ctx.Labels.Notes)}</div>" +
               $"<div>{Html.EscapeMultiline(notes)}</div>" +
               "</div>";
    }

    public static string TermsBlock(RenderContext ctx, string? terms)
    {
        if (string.IsNullOrWhiteSpace(terms))
        {
            return string.Empty;
        }

        return "<div class=\"terms-block\">" +
               $"<div class=\"terms-block__title\">{Html.Escape(ctx.Labels.Terms)}</div>" +
               $"<div>{Html.EscapeMultiline(terms)}</div>" +
               "</div>";
    }

    public static string Timeline(RenderContext ctx, IReadOnlyList<TimelineEntry> entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append("<div class=\"timeline\">");
        foreach (var e in entries)
        {
            var accentClass = Html.TimelineAccentClass(e.Accent);
            sb.Append($"<div class=\"timeline__item {accentClass}\">");
            if (!string.IsNullOrWhiteSpace(e.Time))
            {
                sb.Append($"<div class=\"timeline__time\">{Html.Escape(e.Time)}</div>");
            }

            sb.Append($"<div class=\"timeline__title\">{Html.Escape(e.Title)}</div>");
            if (!string.IsNullOrWhiteSpace(e.Description))
            {
                sb.Append($"<div class=\"timeline__desc\">{Html.Escape(e.Description)}</div>");
            }

            sb.Append("</div>");
        }

        sb.Append("</div>");
        return Section(ctx.Labels.Timeline, sb.ToString());
    }

    public static string Attachments(RenderContext ctx, IReadOnlyList<AttachmentItem> items)
    {
        if (items is null || items.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append("<div class=\"attachments\">");
        foreach (var a in items)
        {
            sb.Append("<div class=\"attachment\">");
            sb.Append("<span class=\"attachment__icon icon icon-doc\"></span>");
            sb.Append($"<span>{Html.Escape(a.Name)}</span>");
            if (!string.IsNullOrWhiteSpace(a.Meta))
            {
                sb.Append($"<span class=\"text-muted text-xs ms-auto\">{Html.Escape(a.Meta)}</span>");
            }

            sb.Append("</div>");
        }

        sb.Append("</div>");
        return Section(ctx.Labels.Attachments, sb.ToString());
    }

    public static string Signatures(RenderContext ctx, IReadOnlyList<SignatureSlot> slots)
    {
        if (slots is null || slots.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append("<div class=\"signatures\">");
        foreach (var s in slots)
        {
            sb.Append("<div class=\"signature\">");
            sb.Append("<div class=\"signature__line\">");
            sb.Append(Html.Escape(s.Title));
            if (!string.IsNullOrWhiteSpace(s.Name))
            {
                sb.Append($"<div class=\"text-xs text-muted\">{Html.Escape(s.Name)}</div>");
            }

            sb.Append("</div>");
            sb.Append("</div>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    public static string StampArea(RenderContext ctx) =>
        $"<div class=\"stamp-area\">{Html.Escape(ctx.Labels.Stamp)}</div>";

    public static string ApprovalBadge(RenderContext ctx, ApprovalInfo? approval)
    {
        if (approval is null)
        {
            return string.Empty;
        }

        var (cls, fallback) = approval.State switch
        {
            ApprovalState.Pending => ("approval approval--pending", ctx.Labels.Pending),
            ApprovalState.Rejected => ("approval approval--rejected", ctx.Labels.Rejected),
            _ => ("approval", ctx.Labels.Approved)
        };

        var label = string.IsNullOrWhiteSpace(approval.Label) ? fallback : approval.Label!;
        var sb = new StringBuilder();
        sb.Append($"<span class=\"{cls}\"><span class=\"icon icon-check\"></span>{Html.Escape(label)}");
        if (!string.IsNullOrWhiteSpace(approval.By))
        {
            sb.Append($" — {Html.Escape(approval.By)}");
        }

        if (!string.IsNullOrWhiteSpace(approval.Date))
        {
            sb.Append($" ({Html.Escape(approval.Date)})");
        }

        sb.Append("</span>");
        return sb.ToString();
    }

    /// <summary>QR placeholder (or QR image when supplied via branding).</summary>
    public static string Qr(RenderContext ctx)
    {
        var b = ctx.Branding;
        if (!string.IsNullOrWhiteSpace(b.QrImageUrl))
        {
            return $"<img class=\"qr\" src=\"{Html.Attr(b.QrImageUrl)}\" alt=\"QR\">";
        }

        if (!string.IsNullOrWhiteSpace(b.QrContent))
        {
            return "<div class=\"qr\"></div>";
        }

        return "<div class=\"qr qr--placeholder\">QR</div>";
    }

    /// <summary>Full-sheet diagonal watermark.</summary>
    public static string Watermark(RenderContext ctx, DocumentModel model)
    {
        var text = model.WatermarkText ?? ctx.Branding.WatermarkText;
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return "<div class=\"watermark\" aria-hidden=\"true\">" +
               $"<span class=\"watermark__text\">{Html.Escape(text)}</span>" +
               "</div>";
    }
}

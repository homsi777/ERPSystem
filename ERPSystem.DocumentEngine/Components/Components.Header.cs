using System.Text;
using ERPSystem.DocumentEngine.Helpers;
using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Services;

namespace ERPSystem.DocumentEngine.Components;

/// <summary>
/// Central, reusable HTML component library. Every template composes documents
/// from these methods so there is zero duplicated markup between templates.
/// </summary>
public static partial class Components
{
    /// <summary>Company header: brand block + document title + number/status.</summary>
    public static string CompanyHeader(RenderContext ctx, DocumentModel model)
    {
        var b = ctx.Branding;
        var sb = new StringBuilder();
        sb.Append("<header class=\"company-header\">");

        // Brand
        sb.Append("<div class=\"company-header__brand\">");
        sb.Append(Logo(ctx));
        sb.Append("<div class=\"company-header__identity\">");
        sb.Append($"<h1 class=\"company-header__name\">{Html.Escape(b.Name)}</h1>");
        if (!string.IsNullOrWhiteSpace(b.Tagline))
        {
            sb.Append($"<div class=\"company-header__tagline\">{Html.Escape(b.Tagline)}</div>");
        }

        sb.Append("<div class=\"company-header__meta\">");
        sb.Append(MetaLine("icon-location", b.Address));
        sb.Append(MetaLine("icon-phone", b.Phone));
        sb.Append(MetaLine("icon-mail", b.Email));
        sb.Append(MetaLine("icon-globe", b.Website));
        sb.Append("</div>");
        sb.Append("</div>");
        sb.Append("</div>");

        // Document title + number + status
        sb.Append("<div class=\"company-header__doc\">");
        sb.Append($"<div class=\"company-header__doc-title\">{Html.Escape(model.Title)}</div>");
        if (!string.IsNullOrWhiteSpace(model.Subtitle))
        {
            sb.Append($"<div class=\"company-header__meta\">{Html.Escape(model.Subtitle)}</div>");
        }

        if (!string.IsNullOrWhiteSpace(model.Number))
        {
            sb.Append(DocumentNumberBadge(model.Number!));
        }

        sb.Append(StatusBadge(model));
        sb.Append("</div>");

        sb.Append("</header>");
        return sb.ToString();
    }

    /// <summary>Logo image or a styled placeholder built from the company initials.</summary>
    public static string Logo(RenderContext ctx)
    {
        var b = ctx.Branding;
        if (!string.IsNullOrWhiteSpace(b.LogoUrl))
        {
            return $"<img class=\"company-header__logo\" src=\"{Html.Attr(b.LogoUrl)}\" alt=\"{Html.Attr(b.Name)}\">";
        }

        var initials = Initials(b.Name);
        return $"<div class=\"company-header__logo company-header__logo--placeholder\">{Html.Escape(initials)}</div>";
    }

    public static string DocumentNumberBadge(string number)
    {
        return "<span class=\"doc-number\">" +
               "<span class=\"doc-number__label\"><span class=\"icon icon-hash\"></span></span>" +
               $"<span class=\"doc-number__value\">{Html.Escape(number)}</span>" +
               "</span>";
    }

    public static string StatusBadge(DocumentModel model)
    {
        if (model.Status == DocumentStatus.None && string.IsNullOrWhiteSpace(model.StatusLabel))
        {
            return string.Empty;
        }

        var (accent, fallback) = Html.StatusMeta(model.Status);
        var label = string.IsNullOrWhiteSpace(model.StatusLabel) ? fallback : model.StatusLabel!;
        if (string.IsNullOrWhiteSpace(label))
        {
            return string.Empty;
        }

        return $"<span class=\"{Html.BadgeClass(accent)} badge--solid\">{Html.Escape(label)}</span>";
    }

    /// <summary>Company footer with legal identifiers + page indicator.</summary>
    public static string CompanyFooter(RenderContext ctx, int pageNumber = 1, int pageCount = 1)
    {
        var b = ctx.Branding;
        var lbl = ctx.Labels;
        var legal = new StringBuilder();

        var identifiers = new List<string>();
        if (!string.IsNullOrWhiteSpace(b.TaxNumber))
        {
            identifiers.Add($"{Html.Escape(lbl.TaxNumber)}: {Html.Escape(b.TaxNumber)}");
        }

        if (!string.IsNullOrWhiteSpace(b.CommercialRegister))
        {
            identifiers.Add($"{Html.Escape(lbl.CommercialRegister)}: {Html.Escape(b.CommercialRegister)}");
        }

        if (identifiers.Count > 0)
        {
            legal.Append(string.Join(" &nbsp;•&nbsp; ", identifiers));
        }

        if (!string.IsNullOrWhiteSpace(b.FooterNote))
        {
            if (legal.Length > 0)
            {
                legal.Append("<br>");
            }

            legal.Append(Html.EscapeMultiline(b.FooterNote));
        }

        return "<footer class=\"doc-footer company-footer\">" +
               $"<div class=\"company-footer__legal\">{legal}</div>" +
               $"<div class=\"company-footer__page\">{Html.Escape(lbl.Page)} {pageNumber} {Html.Escape(lbl.Of)} {pageCount}</div>" +
               "</footer>";
    }

    private static string MetaLine(string icon, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return $"<span class=\"nowrap\"><span class=\"icon {icon}\"></span> {Html.Escape(value)}</span> ";
    }

    private static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "•";
        }

        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
        }

        return (parts[0][0].ToString() + parts[1][0]).ToUpperInvariant();
    }
}

using System.Text;
using ERPSystem.DocumentEngine.Helpers;
using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Services;

namespace ERPSystem.DocumentEngine.Components;

public static partial class Components
{
    /// <summary>Customer / supplier / partner card.</summary>
    public static string PartyCard(PartyInfo? party)
    {
        if (party is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append($"<div class=\"{Html.PartyClass(party.Kind)}\">");

        if (!string.IsNullOrWhiteSpace(party.Role))
        {
            sb.Append($"<div class=\"party-card__role\">{Html.Escape(party.Role)}</div>");
        }

        sb.Append($"<div class=\"party-card__name\">{Html.Escape(party.Name)}</div>");
        sb.Append("<div class=\"party-card__meta\">");

        AppendMeta(sb, "icon-location", party.Address);
        AppendMeta(sb, "icon-phone", party.Phone);
        AppendMeta(sb, "icon-mail", party.Email);

        if (!string.IsNullOrWhiteSpace(party.TaxNumber))
        {
            sb.Append($"<div><span class=\"icon icon-hash\"></span> {Html.Escape(party.TaxNumber)}</div>");
        }

        if (!string.IsNullOrWhiteSpace(party.AccountCode))
        {
            sb.Append($"<div class=\"text-mono\">{Html.Escape(party.AccountCode)}</div>");
        }

        foreach (var line in party.ExtraLines)
        {
            sb.Append($"<div>{Html.Escape(line)}</div>");
        }

        sb.Append("</div>");
        sb.Append("</div>");
        return sb.ToString();
    }

    /// <summary>Header meta grid (issue date, due date, reference, currency, ...).</summary>
    public static string InfoFields(IReadOnlyList<InfoField> fields)
    {
        if (fields is null || fields.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append("<div class=\"card card--flat\"><div class=\"kv\">");
        foreach (var f in fields)
        {
            var valueClass = f.Emphasize ? "kv__val fw-bold" : "kv__val";
            sb.Append($"<div class=\"kv__key\">{Html.Escape(f.Label)}</div>");
            sb.Append($"<div class=\"{valueClass}\">{Html.Escape(f.Value)}</div>");
        }

        sb.Append("</div></div>");
        return sb.ToString();
    }

    private static void AppendMeta(StringBuilder sb, string icon, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        sb.Append($"<div><span class=\"icon {icon}\"></span> {Html.Escape(value)}</div>");
    }
}

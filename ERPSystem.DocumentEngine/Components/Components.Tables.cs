using System.Text;
using ERPSystem.DocumentEngine.Helpers;
using ERPSystem.DocumentEngine.Models;

namespace ERPSystem.DocumentEngine.Components;

public static partial class Components
{
    /// <summary>
    /// Enterprise responsive table. On desktop it is a normal table; on mobile
    /// (via the .responsive class + data-label attributes) each row collapses
    /// into an information card. Horizontal overflow is never produced.
    /// </summary>
    public static string Table(DocumentTable table)
    {
        if (table is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(table.Title))
        {
            sb.Append($"<div class=\"doc-section__title\">{Html.Escape(table.Title)}</div>");
        }

        var classes = "doc-table";
        if (table.Responsive) classes += " responsive";
        if (table.Bordered) classes += " doc-table--bordered";
        if (table.Compact) classes += " doc-table--compact";

        sb.Append("<div class=\"doc-table-wrap\">");
        sb.Append($"<table class=\"{classes}\">");

        // Head
        sb.Append("<thead><tr>");
        foreach (var col in table.Columns)
        {
            sb.Append($"<th class=\"{Html.AlignClass(col.Align)}\"{Html.OptAttr("style", ColWidth(col.Width))}>{Html.Escape(col.Header)}</th>");
        }

        sb.Append("</tr></thead>");

        // Body
        sb.Append("<tbody>");
        foreach (var row in table.Rows)
        {
            sb.Append(row.Highlight ? "<tr class=\"row-highlight\">" : "<tr>");
            for (var i = 0; i < row.Cells.Count; i++)
            {
                var cell = row.Cells[i];
                var col = i < table.Columns.Count ? table.Columns[i] : null;
                var align = cell.Align ?? col?.Align ?? TextAlign.Start;
                var numeric = col?.Numeric == true;
                var cls = Html.AlignClass(align) + (numeric ? " num" : string.Empty);
                var label = col is null ? string.Empty : Html.Attr(col.Header);

                sb.Append($"<td class=\"{cls}\" data-label=\"{label}\">");
                sb.Append(cell.BadgeAccent is { } accent
                    ? $"<span class=\"{Html.BadgeClass(accent)}\">{Html.Escape(cell.Text)}</span>"
                    : Html.Escape(cell.Text));
                sb.Append("</td>");
            }

            sb.Append("</tr>");
        }

        sb.Append("</tbody>");

        // Foot
        if (table.Footer is { Count: > 0 })
        {
            sb.Append("<tfoot><tr>");
            for (var i = 0; i < table.Footer.Count; i++)
            {
                var cell = table.Footer[i];
                var col = i < table.Columns.Count ? table.Columns[i] : null;
                var align = cell.Align ?? col?.Align ?? TextAlign.Start;
                var numeric = col?.Numeric == true;
                var cls = Html.AlignClass(align) + (numeric ? " num" : string.Empty);
                sb.Append($"<td class=\"{cls}\">{Html.Escape(cell.Text)}</td>");
            }

            sb.Append("</tr></tfoot>");
        }

        sb.Append("</table>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string? ColWidth(string? width) =>
        string.IsNullOrWhiteSpace(width) ? null : $"width:{width};";
}

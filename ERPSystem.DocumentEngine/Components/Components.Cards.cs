using System.Text;
using ERPSystem.DocumentEngine.Helpers;
using ERPSystem.DocumentEngine.Models;

namespace ERPSystem.DocumentEngine.Components;

public static partial class Components
{
    /// <summary>Row of KPI / summary cards.</summary>
    public static string SummaryCards(IReadOnlyList<SummaryCard> cards)
    {
        if (cards is null || cards.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append("<div class=\"metrics\">");
        foreach (var c in cards)
        {
            sb.Append($"<div class=\"metric {Html.MetricAccentClass(c.Accent)}\">");
            sb.Append($"<div class=\"metric__label\">{Html.Escape(c.Label)}</div>");
            sb.Append($"<div class=\"metric__value\">{Html.Escape(c.Value)}</div>");
            if (!string.IsNullOrWhiteSpace(c.Delta))
            {
                sb.Append($"<div class=\"metric__delta text-{Html.AccentToken(c.Accent)}\">{Html.Escape(c.Delta)}</div>");
            }

            sb.Append("</div>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    /// <summary>Totals panel (subtotal / discount / tax / grand total).</summary>
    public static string TotalsPanel(TotalsModel? totals)
    {
        if (totals is null || totals.Lines.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append("<div class=\"totals\">");
        foreach (var line in totals.Lines)
        {
            var rowClass = line.IsGrand ? "totals__row totals__row--grand" : "totals__row";
            sb.Append($"<div class=\"{rowClass}\">");
            sb.Append($"<span class=\"totals__label\">{Html.Escape(line.Label)}</span>");
            sb.Append($"<span class=\"totals__value num\">{Html.Escape(line.Value)}</span>");
            sb.Append("</div>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    /// <summary>Tax breakdown panel.</summary>
    public static string TaxPanel(IReadOnlyList<TaxLine> taxes)
    {
        if (taxes is null || taxes.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append("<div class=\"tax-panel\">");
        foreach (var t in taxes)
        {
            sb.Append("<div class=\"tax-panel__row\">");
            var label = t.Label;
            if (!string.IsNullOrWhiteSpace(t.Rate))
            {
                label += $" ({t.Rate})";
            }

            sb.Append($"<span>{Html.Escape(label)}</span>");
            sb.Append($"<span class=\"num\">{Html.Escape(t.Amount)}</span>");
            sb.Append("</div>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }
}

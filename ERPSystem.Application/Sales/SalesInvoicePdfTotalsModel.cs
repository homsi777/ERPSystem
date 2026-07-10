using ERPSystem.Application.DTOs.Sales;

namespace ERPSystem.Application.Sales;

/// <summary>Read-only totals model for invoice PDF — uses stored snapshot fields only.</summary>
public static class SalesInvoicePdfTotalsModel
{
    public sealed record Row(string Label, decimal Amount, bool IsBold = false);

    public sealed record Result(
        bool IsLegacy,
        IReadOnlyList<Row> Rows,
        decimal GrandTotal,
        IReadOnlyList<(string TaxCode, decimal Rate, decimal TaxAmount)> TaxBreakdown);

    public static Result Build(SalesInvoiceDto invoice)
    {
        var rows = new List<Row>();
        if (invoice.IsLegacyUntaxed)
            rows.Add(new Row("Legacy Untaxed", 0m));

        var lineDiscount = invoice.Lines.Sum(l => l.DiscountAmount);
        rows.Add(new Row("Subtotal", invoice.SubTotal));
        if (lineDiscount > 0) rows.Add(new Row("Line discounts", -lineDiscount));
        if (invoice.DiscountTotal != 0m) rows.Add(new Row("Invoice discount", -invoice.DiscountTotal));

        if (!invoice.IsLegacyUntaxed)
        {
            var taxable = invoice.Lines.Sum(l => l.TaxableAmount);
            if (taxable > 0) rows.Add(new Row("Taxable amount", taxable));
        }

        if (invoice.TaxTotal != 0m)
            rows.Add(new Row("Tax total", invoice.TaxTotal));

        var breakdown = invoice.Lines
            .Where(l => l.TaxAmount > 0)
            .GroupBy(l => l.TaxCode)
            .Select(g => (g.Key ?? "—", g.First().TaxRate, g.Sum(x => x.TaxAmount)))
            .ToList();

        foreach (var (code, rate, amount) in breakdown)
            rows.Add(new Row($"  {code} ({rate:P0})", amount));

        if (Math.Abs(invoice.RoundingDifference) >= 0.01m)
            rows.Add(new Row("Rounding difference", invoice.RoundingDifference));

        rows.Add(new Row("Grand total", invoice.GrandTotal, IsBold: true));

        return new Result(invoice.IsLegacyUntaxed, rows, invoice.GrandTotal, breakdown);
    }
}

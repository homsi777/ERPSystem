using ERPSystem.Application.Abstractions.Services;

namespace ERPSystem.Application.Tax;

/// <summary>Central rounding policy for money and tax amounts.</summary>
public static class SalesTaxRounding
{
    public const int MoneyDecimals = 2;
    public const int TaxDecimals = 2;
    public const int QuantityDecimals = 4;
    public const MidpointRounding Mode = MidpointRounding.AwayFromZero;

    public static decimal RoundMoney(decimal value) =>
        Math.Round(value, MoneyDecimals, Mode);

    public static decimal RoundTax(decimal value) =>
        Math.Round(value, TaxDecimals, Mode);
}

public sealed class SalesTaxEngine : ISalesTaxEngine
{
    public SalesTaxCalculationResult Calculate(SalesTaxCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lines = request.Lines;
        var subtotalBeforeDiscount = SalesTaxRounding.RoundMoney(
            lines.Sum(l => l.NetLineAmount + l.LineDiscountTotal));
        var lineDiscountTotal = SalesTaxRounding.RoundMoney(lines.Sum(l => l.LineDiscountTotal));
        var invoiceDiscount = SalesTaxRounding.RoundMoney(Math.Max(0m, request.InvoiceDiscountTotal));

        var allocationBase = lines.Sum(l => l.NetLineAmount);
        if (invoiceDiscount > allocationBase && allocationBase > 0)
            throw new InvalidOperationException("Invoice discount exceeds merchandise subtotal.");

        var lineResults = new List<SalesTaxLineResult>(lines.Count);
        decimal taxTotal = 0m;
        decimal taxableTotal = 0m;
        decimal grandFromLines = 0m;

        foreach (var line in lines)
        {
            var allocated = allocationBase > 0
                ? SalesTaxRounding.RoundMoney(invoiceDiscount * (line.NetLineAmount / allocationBase))
                : 0m;

            var workingAmount = SalesTaxRounding.RoundMoney(line.NetLineAmount - allocated);
            decimal taxable;
            decimal tax;
            decimal lineGrand;

            if (line.IsExempt || line.TaxCodeId is null || line.TaxRate <= 0 && !line.IsZeroRated)
            {
                taxable = workingAmount;
                tax = 0m;
                lineGrand = workingAmount;
            }
            else if (line.IsInclusive)
            {
                var divisor = 1m + line.TaxRate;
                taxable = divisor > 0
                    ? SalesTaxRounding.RoundTax(workingAmount / divisor)
                    : workingAmount;
                tax = SalesTaxRounding.RoundTax(workingAmount - taxable);
                lineGrand = workingAmount;
            }
            else
            {
                taxable = workingAmount;
                tax = SalesTaxRounding.RoundTax(taxable * line.TaxRate);
                lineGrand = SalesTaxRounding.RoundMoney(taxable + tax);
            }

            taxableTotal += taxable;
            taxTotal += tax;
            grandFromLines += lineGrand;

            lineResults.Add(new SalesTaxLineResult
            {
                LineId = line.LineId,
                TaxCodeId = line.TaxCodeId,
                TaxCode = line.TaxCode,
                TaxName = line.TaxName,
                TaxRate = line.TaxRate,
                IsInclusive = line.IsInclusive,
                SalesTaxAccountId = line.SalesTaxAccountId,
                AllocatedInvoiceDiscount = allocated,
                TaxableAmount = taxable,
                TaxAmount = tax,
                LineGrandTotal = lineGrand
            });
        }

        taxTotal = SalesTaxRounding.RoundTax(taxTotal);
        taxableTotal = SalesTaxRounding.RoundMoney(taxableTotal);
        var grandTotal = SalesTaxRounding.RoundMoney(grandFromLines);
        // Taxable + tax is valid for both exclusive and inclusive pricing.
        // Adding tax to allocationBase double-counts VAT on inclusive lines.
        var expectedGrand = SalesTaxRounding.RoundMoney(taxableTotal + taxTotal);
        var roundingDifference = SalesTaxRounding.RoundMoney(grandTotal - expectedGrand);
        if (Math.Abs(roundingDifference) <= 0.01m)
            grandTotal = expectedGrand;

        var summary = lineResults
            .Where(l => l.TaxCodeId is not null)
            .GroupBy(l => new { l.TaxCodeId, l.TaxCode, l.TaxName, l.TaxRate, l.SalesTaxAccountId })
            .Select(g => new SalesTaxSummaryLine
            {
                TaxCodeId = g.Key.TaxCodeId,
                TaxCode = g.Key.TaxCode,
                TaxName = g.Key.TaxName,
                TaxRate = g.Key.TaxRate,
                SalesTaxAccountId = g.Key.SalesTaxAccountId,
                TaxableAmount = SalesTaxRounding.RoundMoney(g.Sum(x => x.TaxableAmount)),
                TaxAmount = SalesTaxRounding.RoundTax(g.Sum(x => x.TaxAmount))
            })
            .ToList();

        return new SalesTaxCalculationResult
        {
            SubtotalBeforeDiscount = subtotalBeforeDiscount,
            LineDiscountTotal = lineDiscountTotal,
            InvoiceDiscountTotal = invoiceDiscount,
            TaxableAmount = taxableTotal,
            TaxTotal = taxTotal,
            GrandTotal = grandTotal,
            RoundingDifference = roundingDifference,
            LineResults = lineResults,
            TaxSummary = summary
        };
    }
}

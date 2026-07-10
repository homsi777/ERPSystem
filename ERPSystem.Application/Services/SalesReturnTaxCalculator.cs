using ERPSystem.Application.Tax;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Sales;

namespace ERPSystem.Application.Services;

public static class SalesReturnTaxCalculator
{
    public sealed record Result(
        decimal TaxTotal,
        decimal CustomerCreditTotal,
        bool IsLegacyUntaxedReturn,
        bool TaxIncludedInLineTotals,
        IReadOnlyList<(Guid AccountId, decimal Amount)> TaxByAccount,
        IReadOnlyList<SalesReturnLineTaxSnapshot> LineSnapshots);

    public sealed record SalesReturnLineTaxSnapshot(
        Guid SalesReturnLineId,
        Guid OriginalInvoiceItemId,
        Guid? TaxCodeId,
        string? TaxCode,
        decimal TaxRate,
        decimal TaxableAmount,
        decimal TaxAmount,
        Guid? SalesTaxAccountId);

    public static Result Compute(SalesReturnAggregate salesReturn, SalesInvoiceAggregate invoice)
    {
        if (invoice.IsLegacyUntaxed || invoice.ItemTaxSnapshots.Count == 0)
        {
            return new Result(
                0m,
                salesReturn.TotalAmount.Amount,
                true,
                false,
                [],
                []);
        }

        var snapshots = new List<SalesReturnLineTaxSnapshot>();
        var taxByAccount = new Dictionary<Guid, decimal>();
        decimal taxTotal = 0m;
        var taxIncluded = false;

        foreach (var line in salesReturn.Lines)
        {
            var itemSnapshot = invoice.ItemTaxSnapshots
                .FirstOrDefault(s => s.SalesInvoiceItemId == line.OriginalInvoiceItemId);
            if (itemSnapshot is null || line.OriginalMeters <= 0)
                continue;

            var ratio = line.ReturnMeters / line.OriginalMeters;
            var lineTax = SalesTaxRounding.RoundTax(itemSnapshot.TaxAmount.Amount * ratio);
            var lineTaxable = SalesTaxRounding.RoundMoney(itemSnapshot.TaxableAmount.Amount * ratio);
            if (lineTax <= 0)
                continue;

            taxIncluded |= itemSnapshot.IsInclusive;
            taxTotal += lineTax;

            if (itemSnapshot.SalesTaxAccountId is Guid accountId)
                taxByAccount[accountId] = taxByAccount.GetValueOrDefault(accountId) + lineTax;

            snapshots.Add(new SalesReturnLineTaxSnapshot(
                line.Id,
                line.OriginalInvoiceItemId,
                itemSnapshot.TaxCodeId,
                itemSnapshot.TaxCode,
                itemSnapshot.TaxRate,
                lineTaxable,
                lineTax,
                itemSnapshot.SalesTaxAccountId));
        }

        taxTotal = SalesTaxRounding.RoundTax(taxTotal);
        var customerCredit = taxIncluded
            ? salesReturn.TotalAmount.Amount
            : salesReturn.TotalAmount.Amount + taxTotal;

        return new Result(
            taxTotal,
            customerCredit,
            false,
            taxIncluded,
            taxByAccount.Select(kv => (kv.Key, kv.Value)).ToList(),
            snapshots);
    }
}

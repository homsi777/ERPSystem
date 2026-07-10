using ERPSystem.Domain.Aggregates;

namespace ERPSystem.Application.Tax;

/// <summary>Builds balanced sales-invoice approval journal lines per <see cref="docs/accounting/SALES_TAX_AND_DISCOUNT_POSTING_POLICY.md"/>.</summary>
public static class SalesInvoiceApprovalPostingBuilder
{
    public sealed record Line(Guid AccountId, decimal Debit, decimal Credit, string Narrative, Guid? PartyId);

    public sealed record Result(
        decimal AccountsReceivableDebit,
        decimal RevenueCredit,
        decimal LineDiscountDebit,
        decimal VatCredit,
        IReadOnlyList<Line> Lines);

    public static Result Build(
        SalesInvoiceAggregate invoice,
        Guid arAccountId,
        Guid revenueAccountId,
        Guid discountAccountId,
        Guid? defaultVatAccountId)
    {
        var netReceivable = invoice.GrandTotal.Amount;
        var lineDiscount = invoice.TotalLineDiscount.Amount;
        var taxTotal = invoice.IsLegacyUntaxed ? 0m : invoice.TaxTotal.Amount;
        var revenueCredit = netReceivable - taxTotal + lineDiscount;

        var lines = new List<Line>
        {
            new(arAccountId, netReceivable, 0m, "ذمم عميل — فاتورة بيع", invoice.CustomerId),
            new(revenueAccountId, 0m, revenueCredit, "إيراد مبيعات أقمشة", null)
        };

        if (lineDiscount > 0)
            lines.Add(new(discountAccountId, lineDiscount, 0m, "خصم مبيعات ممنوح", invoice.CustomerId));

        decimal vatTotal = 0m;
        if (taxTotal > 0)
        {
            foreach (var group in invoice.ItemTaxSnapshots
                         .Where(s => s.TaxAmount.Amount > 0)
                         .GroupBy(s => s.SalesTaxAccountId ?? defaultVatAccountId!.Value))
            {
                var amount = group.Sum(g => g.TaxAmount.Amount);
                vatTotal += amount;
                lines.Add(new(group.Key, 0m, amount, "ضريبة مبيعات مستحقة", null));
            }
        }

        return new Result(netReceivable, revenueCredit, lineDiscount, vatTotal, lines);
    }

    public static decimal TotalDebits(IReadOnlyList<Line> lines) =>
        lines.Sum(l => l.Debit);

    public static decimal TotalCredits(IReadOnlyList<Line> lines) =>
        lines.Sum(l => l.Credit);
}

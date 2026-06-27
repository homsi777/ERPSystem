using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Sales;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Services;

public static class SalesInvoiceTotalCalculator
{
    public static Money CalculateSubTotal(IEnumerable<SalesInvoiceItem> items) =>
        items.Aggregate(Money.Zero(), (sum, item) => sum.Add(item.LineTotal));

    public static Money CalculateGrandTotal(Money subTotal, Money discountTotal, Money taxTotal) =>
        subTotal.Add(taxTotal).Subtract(discountTotal);

    public static Money CalculateForInvoice(SalesInvoiceAggregate invoice) =>
        CalculateGrandTotal(invoice.SubTotal, invoice.DiscountTotal, invoice.TaxTotal);

    public static LengthInMeters CalculateTotalMeters(IEnumerable<SalesInvoiceRollDetail> rollDetails) =>
        rollDetails.Aggregate(LengthInMeters.Zero, (sum, d) => sum.Add(d.LengthMeters));
}

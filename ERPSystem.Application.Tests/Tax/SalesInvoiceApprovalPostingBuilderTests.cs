using ERPSystem.Application.Common;
using ERPSystem.Application.Tax;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.Tests.Tax;

public sealed class SalesInvoiceApprovalPostingBuilderTests
{
    private static readonly Guid Ar = AccountingAccountIds.AccountsReceivable;
    private static readonly Guid Rev = AccountingAccountIds.SalesRevenue;
    private static readonly Guid Disc = AccountingAccountIds.SalesDiscounts;
    private static readonly Guid Vat = AccountingAccountIds.VatPayable;

    [Fact]
    public void Example_A_no_discount_15_percent_tax()
    {
        var invoice = BuildInvoice(subTotal: 1000m, tax: 150m, discount: 0m, lineDiscount: 0m);
        var result = SalesInvoiceApprovalPostingBuilder.Build(invoice, Ar, Rev, Disc, Vat);

        Assert.Equal(1150m, result.AccountsReceivableDebit);
        Assert.Equal(1000m, result.RevenueCredit);
        Assert.Equal(150m, result.VatCredit);
        Assert.Equal(1150m, SalesInvoiceApprovalPostingBuilder.TotalDebits(result.Lines));
        Assert.Equal(1150m, SalesInvoiceApprovalPostingBuilder.TotalCredits(result.Lines));
    }

    [Fact]
    public void Example_B_invoice_discount_net_revenue_policy()
    {
        var invoice = BuildInvoice(subTotal: 1000m, tax: 135m, discount: 100m, lineDiscount: 0m);
        var result = SalesInvoiceApprovalPostingBuilder.Build(invoice, Ar, Rev, Disc, Vat);

        Assert.Equal(1035m, result.AccountsReceivableDebit);
        Assert.Equal(900m, result.RevenueCredit);
        Assert.Equal(135m, result.VatCredit);
        Assert.True(SalesInvoiceApprovalPostingBuilder.TotalDebits(result.Lines) ==
                    SalesInvoiceApprovalPostingBuilder.TotalCredits(result.Lines));
    }

    [Fact]
    public void Example_C_line_discount_contra_revenue()
    {
        var invoice = BuildInvoice(subTotal: 900m, tax: 135m, discount: 0m, lineDiscount: 100m);
        var result = SalesInvoiceApprovalPostingBuilder.Build(invoice, Ar, Rev, Disc, Vat);

        Assert.Equal(1035m, result.AccountsReceivableDebit);
        Assert.Equal(1000m, result.RevenueCredit);
        Assert.Equal(135m, result.VatCredit);
        Assert.Equal(100m, result.LineDiscountDebit);
        Assert.Equal(1135m, SalesInvoiceApprovalPostingBuilder.TotalDebits(result.Lines));
        Assert.Equal(1135m, SalesInvoiceApprovalPostingBuilder.TotalCredits(result.Lines));
    }

    [Fact]
    public void Legacy_invoice_posts_no_vat()
    {
        var invoice = BuildInvoice(subTotal: 320m, tax: 0m, discount: 0m, lineDiscount: 0m, legacy: true);
        var result = SalesInvoiceApprovalPostingBuilder.Build(invoice, Ar, Rev, Disc, Vat);

        Assert.Equal(0m, result.VatCredit);
        Assert.Equal(320m, result.RevenueCredit);
    }

    internal static SalesInvoiceAggregate BuildInvoice(
        decimal subTotal,
        decimal tax,
        decimal discount,
        decimal lineDiscount,
        bool legacy = false)
    {
        var invoice = SalesInvoiceAggregate.CreateDraft(
            new InvoiceNumber("TEST-001"),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentType.Credit,
            Guid.NewGuid());

        var unit = lineDiscount > 0 ? 90m : 100m;
        var original = lineDiscount > 0 ? 100m : 100m;
        var item = SalesInvoiceItem.Create(
            1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1,
            new Money(unit), originalUnitPrice: new Money(original));
        invoice.AddItem(item);

        if (lineDiscount > 0)
        {
            typeof(SalesInvoiceItem).GetProperty(nameof(SalesInvoiceItem.DiscountAmount))!
                .SetValue(item, new Money(lineDiscount));
        }

        if (discount > 0)
            invoice.SetDiscountTotal(new Money(discount));

        var snapshots = new List<SalesInvoiceItemTaxSnapshot>();
        if (tax > 0 && !legacy)
        {
            snapshots.Add(SalesInvoiceItemTaxSnapshot.CreateDraft(
                item.Id, SalesTaxCodeIds.DefaultVat15Exclusive, "VAT15", "VAT 15%", 0.15m,
                new Money(subTotal - discount), new Money(tax), false, Vat));
        }

        invoice.ApplyTaxTotals(
            new Money(tax),
            new Money(subTotal + tax - discount),
            0m,
            snapshots,
            freezeSnapshots: tax > 0);

        if (legacy)
            invoice.MarkLegacyUntaxed();

        // Override subtotal to match test scenario when line totals differ
        typeof(SalesInvoiceAggregate).GetProperty(nameof(SalesInvoiceAggregate.SubTotal))!
            .SetValue(invoice, new Money(subTotal));

        return invoice;
    }
}

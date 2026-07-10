using ERPSystem.Application.Common;
using ERPSystem.Application.Services;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.Tests.Tax;

public sealed class SalesReturnTaxCalculatorTests
{
    [Fact]
    public void Matrix_26_Partial_return_tax_reverses_proportionally()
    {
        var (invoice, itemId) = BuildTaxedInvoice(taxable: 1000m, tax: 150m, inclusive: false);
        var salesReturn = BuildReturn(invoice, itemId, originalMeters: 100m, returnMeters: 50m, unitPrice: 10m);

        var result = SalesReturnTaxCalculator.Compute(salesReturn, invoice);

        Assert.False(result.IsLegacyUntaxedReturn);
        Assert.Equal(75m, result.TaxTotal);
        Assert.Equal(575m, result.CustomerCreditTotal);
        Assert.Single(result.LineSnapshots);
        Assert.Equal(75m, result.LineSnapshots[0].TaxAmount);
    }

    [Fact]
    public void Matrix_27_Full_return_tax_matches_original()
    {
        var (invoice, itemId) = BuildTaxedInvoice(taxable: 1000m, tax: 150m, inclusive: false);
        var salesReturn = BuildReturn(invoice, itemId, originalMeters: 100m, returnMeters: 100m, unitPrice: 10m);

        var result = SalesReturnTaxCalculator.Compute(salesReturn, invoice);

        Assert.Equal(150m, result.TaxTotal);
        Assert.Equal(1150m, result.CustomerCreditTotal);
    }

    [Fact]
    public void Matrix_28_Inclusive_return_tax_included_in_customer_credit()
    {
        var (invoice, itemId) = BuildTaxedInvoice(taxable: 1000m, tax: 150m, inclusive: true);
        var salesReturn = BuildReturn(invoice, itemId, originalMeters: 100m, returnMeters: 50m, unitPrice: 11.5m);

        var result = SalesReturnTaxCalculator.Compute(salesReturn, invoice);

        Assert.True(result.TaxIncludedInLineTotals);
        Assert.Equal(75m, result.TaxTotal);
        Assert.Equal(575m, result.CustomerCreditTotal);
    }

    [Fact]
    public void Matrix_29_Legacy_return_has_zero_tax_reversal()
    {
        var (invoice, itemId) = BuildTaxedInvoice(taxable: 0m, tax: 0m, inclusive: false, legacy: true);
        var salesReturn = BuildReturn(invoice, itemId, originalMeters: 100m, returnMeters: 25m, unitPrice: 10m);

        var result = SalesReturnTaxCalculator.Compute(salesReturn, invoice);

        Assert.True(result.IsLegacyUntaxedReturn);
        Assert.Equal(0m, result.TaxTotal);
        Assert.Equal(250m, result.CustomerCreditTotal);
        Assert.Empty(result.LineSnapshots);
    }

    private static (SalesInvoiceAggregate Invoice, Guid ItemId) BuildTaxedInvoice(
        decimal taxable, decimal tax, bool inclusive, bool legacy = false)
    {
        var invoice = SalesInvoiceAggregate.CreateDraft(
            new InvoiceNumber("RET-TEST"),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            PaymentType.Credit, Guid.NewGuid());

        var item = SalesInvoiceItem.Create(1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1,
            new Money(1000m), originalUnitPrice: new Money(1000m));
        invoice.AddItem(item);

        if (legacy)
        {
            invoice.MarkLegacyUntaxed();
            return (invoice, item.Id);
        }

        var snapshots = new List<SalesInvoiceItemTaxSnapshot>
        {
            SalesInvoiceItemTaxSnapshot.CreateDraft(
                item.Id, SalesTaxCodeIds.DefaultVat15Exclusive, "VAT15", "VAT 15%", 0.15m,
                new Money(taxable), new Money(tax), inclusive, AccountingAccountIds.VatPayable)
        };

        invoice.ApplyTaxTotals(new Money(tax), new Money(taxable + tax), 0m, snapshots, freezeSnapshots: true);
        return (invoice, item.Id);
    }

    private static SalesReturnAggregate BuildReturn(
        SalesInvoiceAggregate invoice,
        Guid itemId,
        decimal originalMeters,
        decimal returnMeters,
        decimal unitPrice)
    {
        var line = SalesReturnLine.Create(
            1, itemId, Guid.NewGuid(), Guid.NewGuid(),
            originalMeters, returnMeters, new Money(unitPrice));

        return SalesReturnAggregate.CreateDraft(
            "SR-001",
            invoice.CompanyId,
            invoice.BranchId,
            invoice.Id,
            invoice.InvoiceNumber.Value,
            invoice.CustomerId,
            invoice.WarehouseId,
            DateTime.UtcNow,
            SalesReturnReason.CustomerRequest,
            null,
            null,
            Guid.NewGuid(),
            [line]);
    }
}

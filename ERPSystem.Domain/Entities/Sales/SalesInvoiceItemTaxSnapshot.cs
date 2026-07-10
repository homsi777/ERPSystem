using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Entities.Sales;

/// <summary>Immutable tax snapshot captured at invoice approval.</summary>
public sealed class SalesInvoiceItemTaxSnapshot
{
    public Guid Id { get; private set; }
    public Guid SalesInvoiceItemId { get; private set; }
    public Guid? TaxCodeId { get; private set; }
    public string? TaxCode { get; private set; }
    public string? TaxName { get; private set; }
    public decimal TaxRate { get; private set; }
    public Money TaxableAmount { get; private set; } = Money.Zero();
    public Money TaxAmount { get; private set; } = Money.Zero();
    public bool IsInclusive { get; private set; }
    public Guid? SalesTaxAccountId { get; private set; }
    public bool IsFrozen { get; private set; }

    private SalesInvoiceItemTaxSnapshot() { }

    public static SalesInvoiceItemTaxSnapshot CreateDraft(
        Guid salesInvoiceItemId,
        Guid? taxCodeId,
        string? taxCode,
        string? taxName,
        decimal taxRate,
        Money taxableAmount,
        Money taxAmount,
        bool isInclusive,
        Guid? salesTaxAccountId) =>
        new()
        {
            Id = Guid.NewGuid(),
            SalesInvoiceItemId = salesInvoiceItemId,
            TaxCodeId = taxCodeId,
            TaxCode = taxCode,
            TaxName = taxName,
            TaxRate = taxRate,
            TaxableAmount = taxableAmount,
            TaxAmount = taxAmount,
            IsInclusive = isInclusive,
            SalesTaxAccountId = salesTaxAccountId,
            IsFrozen = false
        };

    public void Freeze()
    {
        if (IsFrozen)
            return;
        IsFrozen = true;
    }
}

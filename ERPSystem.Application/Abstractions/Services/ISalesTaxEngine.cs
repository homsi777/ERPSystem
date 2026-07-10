namespace ERPSystem.Application.Abstractions.Services;

/// <summary>Deterministic sales tax calculator — no database access.</summary>
public interface ISalesTaxEngine
{
    SalesTaxCalculationResult Calculate(SalesTaxCalculationRequest request);
}

public sealed class SalesTaxCalculationRequest
{
    public required string Currency { get; init; }
    public decimal InvoiceDiscountTotal { get; init; }
    public required IReadOnlyList<SalesTaxLineInput> Lines { get; init; }
}

public sealed class SalesTaxLineInput
{
    public required Guid LineId { get; init; }
    public decimal NetLineAmount { get; init; }
    public decimal LineDiscountTotal { get; init; }
    public Guid? TaxCodeId { get; init; }
    public string? TaxCode { get; init; }
    public string? TaxName { get; init; }
    public decimal TaxRate { get; init; }
    public bool IsInclusive { get; init; }
    public bool IsExempt { get; init; }
    public bool IsZeroRated { get; init; }
    public Guid? SalesTaxAccountId { get; init; }
}

public sealed class SalesTaxCalculationResult
{
    public decimal SubtotalBeforeDiscount { get; init; }
    public decimal LineDiscountTotal { get; init; }
    public decimal InvoiceDiscountTotal { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxTotal { get; init; }
    public decimal GrandTotal { get; init; }
    public decimal RoundingDifference { get; init; }
    public required IReadOnlyList<SalesTaxLineResult> LineResults { get; init; }
    public required IReadOnlyList<SalesTaxSummaryLine> TaxSummary { get; init; }
}

public sealed class SalesTaxLineResult
{
    public required Guid LineId { get; init; }
    public Guid? TaxCodeId { get; init; }
    public string? TaxCode { get; init; }
    public string? TaxName { get; init; }
    public decimal TaxRate { get; init; }
    public bool IsInclusive { get; init; }
    public Guid? SalesTaxAccountId { get; init; }
    public decimal AllocatedInvoiceDiscount { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal LineGrandTotal { get; init; }
}

public sealed class SalesTaxSummaryLine
{
    public Guid? TaxCodeId { get; init; }
    public string? TaxCode { get; init; }
    public string? TaxName { get; init; }
    public decimal TaxRate { get; init; }
    public Guid? SalesTaxAccountId { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
}

using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Sales;

public sealed class TaxCodeDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public decimal Rate { get; init; }
    public TaxPriceMode PriceMode { get; init; }
    public TaxCategory Category { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public bool IsInclusive => PriceMode == TaxPriceMode.Inclusive;
}

public sealed class SalesInvoiceTaxPreviewLineDto
{
    public Guid LineId { get; init; }
    public int LineNumber { get; init; }
    public Guid? TaxCodeId { get; init; }
    public string? TaxCode { get; init; }
    public string? TaxName { get; init; }
    public decimal TaxRate { get; init; }
    public TaxCategory? TaxCategory { get; init; }
    public bool IsInclusive { get; init; }
    public decimal LineDiscountTotal { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal LineGrandTotal { get; init; }
}

public sealed class SalesInvoiceTaxPreviewDto
{
    public decimal SubtotalBeforeDiscount { get; init; }
    public decimal LineDiscountTotal { get; init; }
    public decimal InvoiceDiscountTotal { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxTotal { get; init; }
    public decimal GrandTotal { get; init; }
    public decimal RoundingDifference { get; init; }
    public IReadOnlyList<SalesInvoiceTaxPreviewLineDto> Lines { get; init; } = [];
    public IReadOnlyList<SalesTaxSummaryLineDto> TaxSummary { get; init; } = [];
    public IReadOnlyList<string> ValidationErrors { get; init; } = [];
}

public sealed class SalesTaxSummaryLineDto
{
    public Guid? TaxCodeId { get; init; }
    public string? TaxCode { get; init; }
    public string? TaxName { get; init; }
    public decimal TaxRate { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
}

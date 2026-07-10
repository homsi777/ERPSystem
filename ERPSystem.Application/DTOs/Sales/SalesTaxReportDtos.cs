namespace ERPSystem.Application.DTOs.Sales;

public sealed class SalesTaxReportRowDto
{
    public string InvoiceNumber { get; init; } = "";
    public DateTime InvoiceDate { get; init; }
    public string CustomerName { get; init; } = "";
    public string? TaxCode { get; init; }
    public decimal TaxRate { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public bool IsLegacyUntaxed { get; init; }
    public string? JournalEntryNumber { get; init; }
    public string PostingStatus { get; init; } = "";
}

public sealed class SalesTaxReportSummaryDto
{
    public string? TaxCode { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
}

public sealed class SalesTaxReportDto
{
    public IReadOnlyList<SalesTaxReportRowDto> Rows { get; init; } = [];
    public IReadOnlyList<SalesTaxReportSummaryDto> SummaryByTaxCode { get; init; } = [];
}

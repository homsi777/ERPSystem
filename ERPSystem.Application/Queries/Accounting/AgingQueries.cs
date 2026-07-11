using ERPSystem.Application.DTOs.Accounting;

namespace ERPSystem.Application.Queries.Accounting;

public sealed class GetReceivablesAgingQuery
{
    public Guid CompanyId { get; init; }
}

public sealed class GetPayablesAgingQuery
{
    public Guid CompanyId { get; init; }
}

public sealed class PayablesAgingRowDto
{
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = "";
    public decimal TotalInvoiced { get; init; }
    public decimal Paid { get; init; }
    public decimal Outstanding { get; init; }
    public DateTime? OldestInvoiceDate { get; init; }
    public int DaysOverdue { get; init; }
}

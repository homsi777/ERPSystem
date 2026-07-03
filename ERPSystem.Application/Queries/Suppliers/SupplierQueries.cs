namespace ERPSystem.Application.Queries.Suppliers;

public sealed class GetSupplierListQuery
{
    public Guid CompanyId { get; init; }
    public string? Search { get; init; }
    public string? Country { get; init; }
    public int? PaymentTermsDays { get; init; }
    public bool? HasBalance { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed class GetSupplierDetailsQuery
{
    public Guid SupplierId { get; init; }
}

public sealed class GetSupplierStatementQuery
{
    public Guid SupplierId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public sealed class GetSupplierOperationsCenterQuery
{
    public Guid SupplierId { get; init; }
}

public sealed class GetSupplierInvoiceListQuery
{
    public Guid SupplierId { get; init; }
}

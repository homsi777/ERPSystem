using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Queries.Sales;

public sealed class GetSalesInvoiceListQuery
{
    public Guid CompanyId { get; init; }
    public Guid? BranchId { get; init; }
    public SalesInvoiceStatus? Status { get; init; }
    public Guid? CustomerId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed class GetSalesInvoiceOperationsCenterQuery
{
    public Guid InvoiceId { get; init; }
}

public sealed class GetWarehouseDetailingQueueQuery
{
    public Guid WarehouseId { get; init; }
}

public sealed class GetSalesWarehouseStockQuery
{
    public Guid ContainerId { get; init; }
    public Guid WarehouseId { get; init; }
}

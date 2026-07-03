namespace ERPSystem.Application.Queries.Purchases;

using ERPSystem.Domain.Enums;

public sealed class GetPurchaseInvoiceListQuery
{
    public Guid CompanyId { get; init; }
    public string? Search { get; init; }
    public PurchaseInvoiceStatus? Status { get; init; }
}

public sealed class GetPurchaseInvoiceDetailsQuery
{
    public Guid InvoiceId { get; init; }
}

public sealed class GetPurchaseInvoiceOperationsCenterQuery
{
    public Guid InvoiceId { get; init; }
}

public sealed class GetPurchaseOrderListQuery
{
    public Guid CompanyId { get; init; }
    public PurchaseOrderStatus? Status { get; init; }
}

public sealed class GetPurchaseReturnListQuery
{
    public Guid CompanyId { get; init; }
}

public sealed class GetPurchaseOrderDetailsQuery
{
    public Guid OrderId { get; init; }
}

public sealed class GetPurchaseReturnDetailsQuery
{
    public Guid ReturnId { get; init; }
}

public sealed class GetPostedPurchaseInvoicesForSupplierQuery
{
    public Guid CompanyId { get; init; }
    public Guid SupplierId { get; init; }
}

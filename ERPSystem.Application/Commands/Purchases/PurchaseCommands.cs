namespace ERPSystem.Application.Commands.Purchases;

public sealed class CreatePurchaseInvoiceDraftCommand
{
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public Guid SupplierId { get; init; }
    public string? SupplierReference { get; init; }
    public DateTime InvoiceDate { get; init; }
    public DateTime DueDate { get; init; }
    public Guid? WarehouseId { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<PurchaseInvoiceLineInput> Lines { get; init; } = [];
}

public sealed class UpdatePurchaseInvoiceDraftCommand
{
    public Guid InvoiceId { get; init; }
    public Guid SupplierId { get; init; }
    public string? SupplierReference { get; init; }
    public DateTime InvoiceDate { get; init; }
    public DateTime DueDate { get; init; }
    public Guid? WarehouseId { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<PurchaseInvoiceLineInput> Lines { get; init; } = [];
}

public sealed class PostPurchaseInvoiceCommand
{
    public Guid InvoiceId { get; init; }
    public Guid UserId { get; init; }
}

public sealed class CancelPurchaseInvoiceCommand
{
    public Guid InvoiceId { get; init; }
}

public sealed class PurchaseInvoiceLineInput
{
    public int LineType { get; init; }
    public Guid? FabricItemId { get; init; }
    public Guid? FabricColorId { get; init; }
    public Guid? ExpenseAccountId { get; init; }
    public string Description { get; init; } = "";
    public decimal QuantityMeters { get; init; }
    public int RollCount { get; init; } = 1;
    public decimal UnitPrice { get; init; }
}

public sealed class CreatePurchaseOrderCommand
{
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid SupplierId { get; init; }
    public DateTime OrderDate { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<PurchaseOrderLineInput> Lines { get; init; } = [];
}

public sealed class PurchaseOrderLineInput
{
    public Guid? FabricItemId { get; init; }
    public string Description { get; init; } = "";
    public decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
}

public sealed class UpdatePurchaseOrderCommand
{
    public Guid OrderId { get; init; }
    public Guid SupplierId { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<PurchaseOrderLineInput> Lines { get; init; } = [];
}

public sealed class SendPurchaseOrderCommand
{
    public Guid OrderId { get; init; }
}

public sealed class CancelPurchaseOrderCommand
{
    public Guid OrderId { get; init; }
}

public sealed class ConvertPurchaseOrderToInvoiceCommand
{
    public Guid OrderId { get; init; }
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
}

public sealed class CreatePurchaseReturnCommand
{
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid OriginalInvoiceId { get; init; }
    public DateTime ReturnDate { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<PurchaseReturnLineInput> Lines { get; init; } = [];
}

public sealed class PostPurchaseReturnCommand
{
    public Guid ReturnId { get; init; }
}

public sealed class UpdatePurchaseReturnDraftCommand
{
    public Guid ReturnId { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<PurchaseReturnLineInput> Lines { get; init; } = [];
}

public sealed class PurchaseReturnLineInput
{
    public Guid OriginalInvoiceItemId { get; init; }
    public int LineType { get; init; }
    public Guid? FabricItemId { get; init; }
    public Guid? FabricColorId { get; init; }
    public decimal QuantityMeters { get; init; }
    public decimal UnitPrice { get; init; }
}

/// <summary>Backfill purchase invoices for approved China containers missing a financial mirror.</summary>
public sealed class BackfillChinaContainerPurchaseInvoicesCommand
{
    public Guid CompanyId { get; init; }
    public Guid UserId { get; init; }
}

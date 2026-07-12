namespace ERPSystem.Infrastructure.Persistence.Models.Purchasing;

public class PurchaseInvoiceEntity : CancellablePersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public Guid SupplierId { get; set; }
    public string? SupplierReference { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public Guid? WarehouseId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Remaining { get; set; }
    public int Status { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string? Notes { get; set; }
    public DateTime? PostedAt { get; set; }
    public Guid? PostedByUserId { get; set; }
    public Guid? SourceContainerId { get; set; }
}

public class PurchaseInvoiceItemEntity : PersistenceEntity
{
    public Guid PurchaseInvoiceId { get; set; }
    public int LineType { get; set; }
    public Guid? FabricItemId { get; set; }
    public Guid? FabricColorId { get; set; }
    public Guid? ExpenseAccountId { get; set; }
    public string Description { get; set; } = "";
    public decimal QuantityMeters { get; set; }
    public int RollCount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class PurchaseOrderEntity : CancellablePersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string OrderNumber { get; set; } = "";
    public Guid SupplierId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int Status { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseOrderLineEntity : PersistenceEntity
{
    public Guid PurchaseOrderId { get; set; }
    public Guid? FabricItemId { get; set; }
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}

public class PurchaseReturnEntity : CancellablePersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string ReturnNumber { get; set; } = "";
    public Guid OriginalInvoiceId { get; set; }
    public DateTime ReturnDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? PostedAt { get; set; }
}

public class PurchaseReturnLineEntity : PersistenceEntity
{
    public Guid PurchaseReturnId { get; set; }
    public Guid OriginalInvoiceItemId { get; set; }
    public int LineType { get; set; }
    public Guid? FabricItemId { get; set; }
    public Guid? FabricColorId { get; set; }
    public decimal QuantityMeters { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class PurchaseInvoicePaymentEntity : PersistenceEntity
{
    public Guid PurchaseInvoiceId { get; set; }
    public Guid PaymentVoucherId { get; set; }
    public decimal Amount { get; set; }
    public DateTime AppliedAt { get; set; }
}

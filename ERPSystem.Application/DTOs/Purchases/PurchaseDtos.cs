using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Purchases;

public sealed class PurchaseInvoiceListDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public DateTime InvoiceDate { get; init; }
    public DateTime DueDate { get; init; }
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = "";
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public PurchaseInvoiceStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
    public bool IsOverdue { get; init; }
}

public sealed class PurchaseInvoiceDetailsDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = "";
    public int SupplierPaymentTermsDays { get; init; }
    public string? SupplierReference { get; init; }
    public DateTime InvoiceDate { get; init; }
    public DateTime DueDate { get; init; }
    public Guid? WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public decimal SubTotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public PurchaseInvoiceStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
    public Guid? PurchaseOrderId { get; init; }
    public string? Notes { get; init; }
    public DateTime? PostedAt { get; init; }
    public bool IsReadOnly { get; init; }
    public IReadOnlyList<PurchaseInvoiceLineDto> Lines { get; init; } = [];
}

public sealed class PurchaseInvoiceLineDto
{
    public Guid Id { get; init; }
    public PurchaseLineType LineType { get; init; }
    public Guid? FabricItemId { get; init; }
    public string? FabricItemName { get; init; }
    public Guid? FabricColorId { get; init; }
    public Guid? ExpenseAccountId { get; init; }
    public string Description { get; init; } = "";
    public decimal QuantityMeters { get; init; }
    public int RollCount { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}

public sealed class PurchaseOperationsCenterDto
{
    public PurchaseInvoiceDetailsDto Invoice { get; init; } = null!;
    public int DaysUntilDue { get; init; }
    public bool IsOverdue { get; init; }
    public IReadOnlyList<PurchaseJournalEntryDto> JournalEntries { get; init; } = [];
    public IReadOnlyList<PurchasePaymentDto> Payments { get; init; } = [];
}

public sealed class PurchaseJournalEntryDto
{
    public string EntryNumber { get; init; } = "";
    public DateTime EntryDate { get; init; }
    public string Description { get; init; } = "";
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
}

public sealed class PurchasePaymentDto
{
    public Guid VoucherId { get; init; }
    public string VoucherNumber { get; init; } = "";
    public DateTime VoucherDate { get; init; }
    public decimal Amount { get; init; }
    public string StatusDisplay { get; init; } = "";
}

public sealed class PurchaseOrderListDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = "";
    public string SupplierName { get; init; } = "";
    public DateTime OrderDate { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public decimal TotalAmount { get; init; }
    public PurchaseOrderStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
}

public sealed class PurchaseReturnListDto
{
    public Guid Id { get; init; }
    public string ReturnNumber { get; init; } = "";
    public string OriginalInvoiceNumber { get; init; } = "";
    public DateTime ReturnDate { get; init; }
    public decimal TotalAmount { get; init; }
    public PurchaseReturnStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
}

public sealed class PurchaseOrderDetailsDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = "";
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = "";
    public DateTime OrderDate { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public PurchaseOrderStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
    public decimal TotalAmount { get; init; }
    public string? Notes { get; init; }
    public bool IsReadOnly { get; init; }
    public IReadOnlyList<PurchaseOrderLineDto> Lines { get; init; } = [];
}

public sealed class PurchaseOrderLineDto
{
    public Guid Id { get; init; }
    public Guid? FabricItemId { get; init; }
    public string? FabricItemName { get; init; }
    public string Description { get; init; } = "";
    public decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
    public decimal LineTotal { get; init; }
}

public sealed class PurchaseReturnDetailsDto
{
    public Guid Id { get; init; }
    public string ReturnNumber { get; init; } = "";
    public Guid OriginalInvoiceId { get; init; }
    public string OriginalInvoiceNumber { get; init; } = "";
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = "";
    public DateTime ReturnDate { get; init; }
    public PurchaseReturnStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
    public decimal TotalAmount { get; init; }
    public string? Notes { get; init; }
    public bool IsReadOnly { get; init; }
    public IReadOnlyList<PurchaseReturnLineDto> Lines { get; init; } = [];
}

public sealed class PurchaseReturnLineDto
{
    public Guid Id { get; init; }
    public Guid OriginalInvoiceItemId { get; init; }
    public PurchaseLineType LineType { get; init; }
    public Guid? FabricItemId { get; init; }
    public string? FabricItemName { get; init; }
    public Guid? FabricColorId { get; init; }
    public string Description { get; init; } = "";
    public decimal QuantityMeters { get; init; }
    public decimal MaxQuantityMeters { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}

public sealed class PurchaseFabricPickDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string Display => $"{Code} — {NameAr}";
}

public sealed class PurchaseFabricColorPickDto
{
    public Guid Id { get; init; }
    public string ColorCode { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string Display => $"{ColorCode} — {NameAr}";
}

public sealed class PurchaseInvoicePickDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public decimal RemainingAmount { get; init; }
    public string Display => $"{InvoiceNumber} — {RemainingAmount:N2} $";
}

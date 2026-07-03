namespace ERPSystem.Domain.Enums;

public enum PurchaseInvoiceStatus
{
    Draft = 0,
    Approved = 1,
    Posted = 2,
    Cancelled = 3,
    PartiallyPaid = 4,
    Paid = 5
}

public enum PurchaseLineType
{
    Inventory = 0,
    Expense = 1
}

public enum PurchaseOrderStatus
{
    Draft = 0,
    Sent = 1,
    Received = 2,
    Cancelled = 3
}

public enum PurchaseReturnStatus
{
    Draft = 0,
    Posted = 1,
    Cancelled = 2
}

namespace ERPSystem.Application.Notifications;

public sealed class SalesInvoiceDetailedNotification
{
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public decimal GrandTotal { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class ContainerApprovedNotification
{
    public Guid ContainerId { get; init; }
    public string ContainerNumber { get; init; } = "";
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class CustomerCreditLimitExceededNotification
{
    public Guid CustomerId { get; init; }
    public decimal RequestedAmount { get; init; }
    public decimal CreditLimit { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class SalesInvoiceApprovedNotification
{
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public Guid CustomerId { get; init; }
    public decimal GrandTotal { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class JournalEntryPostedNotification
{
    public Guid EntryId { get; init; }
    public string EntryNumber { get; init; } = "";
    public decimal DebitTotal { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class ReceiptVoucherPostedNotification
{
    public Guid VoucherId { get; init; }
    public string VoucherNumber { get; init; } = "";
    public decimal Amount { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class WarehouseStockLowNotification
{
    public Guid WarehouseId { get; init; }
    public Guid FabricItemId { get; init; }
    public decimal AvailableMeters { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class InventoryChangedNotification
{
    public Guid? ContainerId { get; init; }
    public Guid? WarehouseId { get; init; }
    public Guid? FabricItemId { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class CustomerCreatedNotification
{
    public Guid CustomerId { get; init; }
    public string CustomerCode { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class CustomerUpdatedNotification
{
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class CustomerDeactivatedNotification
{
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class ExpenseAwaitingApprovalNotification
{
    public Guid ExpenseId { get; init; }
    public string ExpenseCode { get; init; } = "";
    public string ExpenseName { get; init; } = "";
    public decimal AmountBase { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class ExpensePaymentDueSoonNotification
{
    public Guid ExpenseId { get; init; }
    public string ExpenseCode { get; init; } = "";
    public DateTime DueDate { get; init; }
    public decimal AmountBase { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class ExpensePaymentOverdueNotification
{
    public Guid ExpenseId { get; init; }
    public string ExpenseCode { get; init; } = "";
    public DateTime DueDate { get; init; }
    public decimal AmountBase { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class ExpenseCancelledNotification
{
    public Guid ExpenseId { get; init; }
    public string ExpenseCode { get; init; } = "";
    public string? Reason { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class ExpenseArchivedNotification
{
    public Guid ExpenseId { get; init; }
    public string ExpenseCode { get; init; } = "";
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class RecurringExpenseDueNotification
{
    public Guid ExpenseId { get; init; }
    public string ExpenseCode { get; init; } = "";
    public DateTime DueDate { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class CapitalPartnerCreatedNotification
{
    public Guid PartnerId { get; init; }
    public string PartnerCode { get; init; } = "";
    public string PartnerName { get; init; } = "";
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed class CapitalDistributionAwaitingApprovalNotification
{
    public Guid DistributionId { get; init; }
    public string DistributionCode { get; init; } = "";
    public decimal NetAmount { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

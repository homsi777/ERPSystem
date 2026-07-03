namespace ERPSystem.Domain.Enums;

public enum ExpenseStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Scheduled = 3,
    PartiallyPaid = 4,
    Paid = 5,
    Closed = 6,
    Cancelled = 7,
    Archived = 8
}

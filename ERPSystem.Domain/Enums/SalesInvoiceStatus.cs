namespace ERPSystem.Domain.Enums;

public enum SalesInvoiceStatus
{
    Draft = 0,
    AwaitingDetailing = 1,
    Detailed = 2,
    ReadyForApproval = 3,
    Approved = 4,
    Printed = 5,
    Delivered = 6,
    Cancelled = 7
}

namespace ERPSystem.Domain.Enums;

public enum ApprovalStatus
{
    NotRequired = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum LandingCostStatus
{
    Draft = 0,
    Reviewed = 1,
    Approved = 2
}

public enum FabricRollStatus
{
    Available = 0,
    Reserved = 1,
    Sold = 2,
    Wasted = 3,
    InTransit = 4
}

public enum PurchaseInvoiceStatus
{
    Draft = 0,
    Approved = 1,
    Posted = 2,
    Cancelled = 3
}

namespace ERPSystem.Application.DTOs.Dashboard;

public sealed class DashboardSummaryDto
{
    public int PendingContainersCount { get; init; }
    public int AwaitingDetailingCount { get; init; }
    public int ReadyForApprovalInvoicesCount { get; init; }
    public int OpenReceiptsCount { get; init; }
    /// <summary>Sum of computed customer balances (opening + sales − receipts).</summary>
    public decimal TotalCustomerOutstanding { get; init; }
    public decimal TotalPostedReceipts { get; init; }
    public decimal TotalSupplierPayables { get; init; }
    /// <summary>Active registered customers (matches customer list count).</summary>
    public int ActiveCustomersCount { get; init; }
    public decimal TodaySalesTotal { get; init; }
    public int LowStockItemsCount { get; init; }
    public IReadOnlyList<DashboardActivityDto> RecentActivity { get; init; } = [];
}

public sealed class DashboardActivityDto
{
    public DateTime OccurredAt { get; init; }
    public string EntityType { get; init; } = "";
    public Guid EntityId { get; init; }
    public string Description { get; init; } = "";
}

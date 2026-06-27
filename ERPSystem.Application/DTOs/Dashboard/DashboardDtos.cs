namespace ERPSystem.Application.DTOs.Dashboard;

public sealed class DashboardSummaryDto
{
    public int PendingContainersCount { get; init; }
    public int AwaitingDetailingCount { get; init; }
    public int ReadyForApprovalInvoicesCount { get; init; }
    public int OpenReceiptsCount { get; init; }
    public decimal TotalCustomerOutstanding { get; init; }
    public decimal TodaySalesTotal { get; init; }
    public int LowStockItemsCount { get; init; }
}

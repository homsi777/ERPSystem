namespace ERPSystem.Application.Queries.Dashboard;

public sealed class GetDashboardSummaryQuery
{
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
}

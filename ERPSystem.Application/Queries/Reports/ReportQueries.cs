namespace ERPSystem.Application.Queries.Reports;

public sealed class GetReportPreviewQuery
{
    public string ReportCode { get; init; } = "";
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public Dictionary<string, string> Parameters { get; init; } = new();
}

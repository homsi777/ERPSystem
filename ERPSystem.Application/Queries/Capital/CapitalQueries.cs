using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Queries.Capital;

public sealed class GetCapitalPartnerListQuery
{
    public Guid CompanyId { get; init; }
    public CapitalPartnerListFilter Filter { get; init; } = new();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed class GetCapitalPartnerDetailsQuery
{
    public Guid PartnerId { get; init; }
}

public sealed class GetCapitalPartnerOperationsCenterQuery
{
    public Guid PartnerId { get; init; }
}

public sealed class GetCapitalDashboardQuery
{
    public Guid CompanyId { get; init; }
}

public sealed class GetCapitalPartnerAuditTrailQuery
{
    public Guid PartnerId { get; init; }
}

public sealed class GetCapitalPartnerTimelineQuery
{
    public Guid PartnerId { get; init; }
}

public sealed class GetCapitalReportQuery
{
    public Guid CompanyId { get; init; }
    public string ReportType { get; init; } = "Summary";
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public Guid? PartnerId { get; init; }
    public PartnershipScope? Scope { get; init; }
}

public sealed class GetProfitDistributionListQuery
{
    public Guid CompanyId { get; init; }
}

public sealed class GetCapitalTransactionsQuery
{
    public Guid CompanyId { get; init; }
    public CapitalTransactionListFilter Filter { get; init; } = new();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 100;
}

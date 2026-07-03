using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Capital;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Abstractions.Repositories;

public sealed class CapitalPartnerListFilter
{
    public string? Search { get; init; }
    public PartnerStatus? Status { get; init; }
    public PartnershipScope? Scope { get; init; }
    public bool IncludeArchived { get; init; }
}

public sealed class CapitalTransactionListFilter
{
    public string? Search { get; init; }
    public Guid? PartnerId { get; init; }
    public CapitalTransactionType? Type { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public sealed record CapitalPartnerWithAudit(
    CapitalPartnerAggregate Aggregate,
    DateTime CreatedAt,
    string? CreatedByName,
    DateTime? UpdatedAt);

public interface ICapitalPartnerRepository
{
    Task<CapitalPartnerAggregate?> GetByIdAsync(Guid id, bool includeChildren = false, CancellationToken cancellationToken = default);
    Task<CapitalPartnerWithAudit?> GetWithAuditAsync(Guid id, bool includeChildren = false, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<CapitalPartnerAggregate> Items, int TotalCount)> GetPagedAsync(
        Guid companyId, CapitalPartnerListFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(CapitalPartnerAggregate aggregate, CancellationToken cancellationToken = default);
    Task UpdateAsync(CapitalPartnerAggregate aggregate, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAuditEntryAsync(PartnerAuditEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PartnerAuditEntry>> GetAuditTrailAsync(Guid partnerId, CancellationToken cancellationToken = default);
    Task AddTimelineEventAsync(PartnerTimelineEvent entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PartnerTimelineEvent>> GetTimelineAsync(Guid partnerId, CancellationToken cancellationToken = default);
    Task<CapitalDashboardData> GetDashboardDataAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<ProfitDistribution?> GetDistributionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddDistributionAsync(ProfitDistribution distribution, CancellationToken cancellationToken = default);
    Task UpdateDistributionAsync(ProfitDistribution distribution, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProfitDistribution>> GetDistributionsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<CapitalTransactionRow> Items, int TotalCount)> GetTransactionsPagedAsync(
        Guid companyId,
        CapitalTransactionListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public sealed class CapitalTransactionRow
{
    public Guid Id { get; init; }
    public Guid PartnerId { get; init; }
    public string PartnerCode { get; init; } = "";
    public string PartnerName { get; init; } = "";
    public CapitalTransactionType Type { get; init; }
    public decimal AmountOriginal { get; init; }
    public string Currency { get; init; } = "SAR";
    public decimal SignedBaseAmount { get; init; }
    public DateTime TransactionDate { get; init; }
    public string? Notes { get; init; }
}

public sealed class CapitalDashboardData
{
    public decimal TotalCapitalBase { get; init; }
    public int ActivePartnersCount { get; init; }
    public int ActiveParticipationsCount { get; init; }
    public decimal MonthlyDistributedProfit { get; init; }
    public decimal PendingSettlementsBase { get; init; }
    public string LargestInvestorName { get; init; } = "";
    public decimal LargestInvestorBase { get; init; }
    public IReadOnlyList<CapitalScopeBreakdownPoint> ScopeBreakdown { get; init; } = [];
    public IReadOnlyList<CapitalCurrencyBreakdownPoint> CurrencyBreakdown { get; init; } = [];
    public IReadOnlyList<CapitalMonthlyTrendPoint> InvestmentTrend { get; init; } = [];
    public IReadOnlyList<CapitalTopInvestorPoint> TopInvestors { get; init; } = [];
    public IReadOnlyList<CapitalPendingDistributionPoint> PendingDistributions { get; init; } = [];
}

public sealed class CapitalScopeBreakdownPoint
{
    public PartnershipScope Scope { get; init; }
    public decimal AmountBase { get; init; }
}

public sealed class CapitalCurrencyBreakdownPoint
{
    public string Currency { get; init; } = "";
    public decimal AmountOriginal { get; init; }
    public decimal AmountBase { get; init; }
}

public sealed class CapitalMonthlyTrendPoint
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal AmountBase { get; init; }
}

public sealed class CapitalTopInvestorPoint
{
    public Guid PartnerId { get; init; }
    public string PartnerName { get; init; } = "";
    public decimal CapitalBase { get; init; }
}

public sealed class CapitalPendingDistributionPoint
{
    public Guid DistributionId { get; init; }
    public string Code { get; init; } = "";
    public DistributionStatus Status { get; init; }
    public decimal NetAmount { get; init; }
}

using ERPSystem.Domain.Entities.Finance;

namespace ERPSystem.Application.Queries.Finance;

public sealed class OpeningBalanceListFilter
{
    public OpeningBalanceType? Type { get; init; }
    public OpeningBalanceStatus? Status { get; init; }
    public string? Search { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public Guid? PartyId { get; init; }
    public string? PartySearch { get; init; }
    public decimal? AmountFrom { get; init; }
    public decimal? AmountTo { get; init; }
    public bool IncludeArchived { get; init; }
}

public sealed class GetCustomerOpeningBalanceSummaryQuery
{
    public Guid CompanyId { get; init; }
    public OpeningBalanceListFilter Filter { get; init; } = new();
}

public sealed class GetOpeningBalanceListQuery
{
    public Guid CompanyId { get; init; }
    public OpeningBalanceListFilter Filter { get; init; } = new();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed class GetOpeningBalanceDetailsQuery
{
    public Guid DocumentId { get; init; }
}

public sealed class GetOpeningBalanceDashboardQuery
{
    public Guid CompanyId { get; init; }
}

public sealed class GetOpeningBalanceLookupsQuery
{
    public Guid CompanyId { get; init; }
}

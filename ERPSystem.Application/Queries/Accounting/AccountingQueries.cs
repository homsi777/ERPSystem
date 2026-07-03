using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Queries.Accounting;

public sealed class GetAccountListQuery
{
    public Guid CompanyId { get; init; }
    public string? Search { get; init; }
    public GlAccountType? AccountType { get; init; }
    public bool ActiveOnly { get; init; } = true;
}

public sealed class GetAccountDetailsQuery
{
    public Guid AccountId { get; init; }
}

public sealed class GetPostableAccountsQuery
{
    public Guid CompanyId { get; init; }
}

public sealed class GetJournalEntryListQuery
{
    public Guid CompanyId { get; init; }
    public JournalEntryListFilter Filter { get; init; } = new();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed class GetJournalEntryDetailsQuery
{
    public Guid EntryId { get; init; }
}

public sealed class GetJournalBooksQuery
{
    public Guid CompanyId { get; init; }
}

public sealed class GetTrialBalanceQuery
{
    public Guid CompanyId { get; init; }
    public DateTime AsOfDate { get; init; } = DateTime.Today;
}

public sealed class GetAccountLedgerQuery
{
    public Guid AccountId { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; } = DateTime.Today;
}

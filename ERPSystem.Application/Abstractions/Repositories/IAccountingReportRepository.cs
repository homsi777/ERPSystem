using ERPSystem.Domain.Entities.Accounting;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IJournalBookRepository
{
    Task<IReadOnlyList<JournalBook>> GetListAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<JournalBook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IAccountingReportRepository
{
    Task<IReadOnlyList<TrialBalanceRow>> GetTrialBalanceAsync(
        Guid companyId,
        DateTime asOfDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountLedgerRow>> GetAccountLedgerAsync(
        Guid accountId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    Task<decimal> GetLiabilityAccountBalanceBeforeAsync(
        Guid accountId,
        DateTime beforeDate,
        CancellationToken cancellationToken = default);

    Task<decimal> GetPartyOpeningBalanceAsync(
        Guid partyId,
        DocumentType sourceType,
        CancellationToken cancellationToken = default);
}

public sealed class TrialBalanceRow
{
    public Guid AccountId { get; init; }
    public string AccountCode { get; init; } = "";
    public string AccountName { get; init; } = "";
    public GlAccountType AccountType { get; init; }
    public decimal DebitTotal { get; init; }
    public decimal CreditTotal { get; init; }
    public decimal Balance { get; init; }
}

public sealed class AccountLedgerRow
{
    public Guid JournalEntryId { get; init; }
    public string EntryNumber { get; init; } = "";
    public DateTime EntryDate { get; init; }
    public string Description { get; init; } = "";
    public string LineNarrative { get; init; } = "";
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal RunningBalance { get; init; }
    public DocumentType? SourceType { get; init; }
}

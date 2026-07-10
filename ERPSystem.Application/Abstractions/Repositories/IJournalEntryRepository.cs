using ERPSystem.Application.Posting;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IJournalEntryRepository
{
    Task<AccountingAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountingAggregate?> GetByNumberAsync(string entryNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountingAggregate>> GetListAsync(
        Guid companyId,
        JournalEntryStatus? status = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(
        AccountingAggregate entry,
        Guid companyId,
        Guid branchId,
        JournalEntryPostMetadata? postingMetadata = null,
        CancellationToken cancellationToken = default);
    Task UpdateAsync(AccountingAggregate entry, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<JournalEntryListRow> Items, int TotalCount)> GetPagedAsync(
        Guid companyId,
        JournalEntryListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JournalEntryListRow>> GetBySourceIdAsync(
        Guid sourceId,
        CancellationToken cancellationToken = default);
}

public sealed class JournalEntryListRow
{
    public Guid Id { get; init; }
    public string EntryNumber { get; init; } = "";
    public DateTime EntryDate { get; init; }
    public string Description { get; init; } = "";
    public JournalEntryStatus Status { get; init; }
    public decimal DebitTotal { get; init; }
    public decimal CreditTotal { get; init; }
    public int LineCount { get; init; }
    public DocumentType? SourceType { get; init; }
}

public sealed class JournalEntryListFilter
{
    public string? Search { get; init; }
    public JournalEntryStatus? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

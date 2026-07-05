using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.Queries.Finance;
using ERPSystem.Domain.Entities.Finance;

namespace ERPSystem.Application.Abstractions.Repositories;

/// <summary>
/// Persistence gateway for opening balance documents, lines and events.
/// Only the OpeningBalanceEngine may mutate these tables.
/// </summary>
public interface IOpeningBalanceRepository
{
    Task AddAsync(OpeningBalanceDocument document, CancellationToken cancellationToken = default);
    Task UpdateAsync(OpeningBalanceDocument document, CancellationToken cancellationToken = default);
    Task<OpeningBalanceDocument?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<OpeningBalanceDocument> Items, int TotalCount)> GetPagedAsync(
        Guid companyId,
        OpeningBalanceListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpeningBalanceDocument>> GetAllAsync(
        Guid companyId,
        OpeningBalanceType? type = null,
        CancellationToken cancellationToken = default);

    Task AddEventAsync(OpeningBalanceEvent evt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OpeningBalanceEvent>> GetEventsAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Keys already used by other non-archived documents of the same type
    /// (party ids, account ids, or warehouse+item+color+batch composites) —
    /// used by the duplicate-prevention validator.
    /// </summary>
    Task<IReadOnlySet<string>> GetExistingLineKeysAsync(
        Guid companyId,
        OpeningBalanceType type,
        Guid? excludeDocumentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpeningBalanceJournalLineDto>> GetJournalLinesAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<CustomerOpeningBalanceSummaryDto> GetSummaryAsync(
        Guid companyId,
        OpeningBalanceListFilter filter,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only lookups used by manual entry, Excel matching and validation
/// (customers, suppliers, partners, cashboxes, warehouses, postable accounts).
/// </summary>
public interface IOpeningBalanceLookupService
{
    Task<OpeningBalanceLookupsDto> GetLookupsAsync(Guid companyId, CancellationToken cancellationToken = default);
}

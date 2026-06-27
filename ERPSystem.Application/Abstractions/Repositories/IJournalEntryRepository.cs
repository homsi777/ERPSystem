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
    Task AddAsync(AccountingAggregate entry, CancellationToken cancellationToken = default);
    Task UpdateAsync(AccountingAggregate entry, CancellationToken cancellationToken = default);
}

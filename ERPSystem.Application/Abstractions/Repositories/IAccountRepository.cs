using ERPSystem.Domain.Entities.Accounting;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Account?> GetByCodeAsync(Guid companyId, string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Account>> GetListAsync(
        Guid companyId,
        string? search = null,
        GlAccountType? accountType = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<bool> HasJournalLinesAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task AddAsync(Account account, CancellationToken cancellationToken = default);
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
}

using ERPSystem.Domain.Entities.Finance;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface ICashboxRepository
{
    Task<Cashbox?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cashbox>> GetListAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task AddAsync(Cashbox cashbox, CancellationToken cancellationToken = default);
    Task UpdateAsync(Cashbox cashbox, CancellationToken cancellationToken = default);
}

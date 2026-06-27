using ERPSystem.Domain.Aggregates;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface ISupplierRepository
{
    Task<SupplierAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierAggregate>> GetListAsync(
        Guid companyId,
        string? search = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(SupplierAggregate aggregate, CancellationToken cancellationToken = default);
    Task UpdateAsync(SupplierAggregate aggregate, CancellationToken cancellationToken = default);
}

using ERPSystem.Domain.Aggregates;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IWarehouseRepository
{
    Task<WarehouseAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseAggregate>> GetListAsync(
        Guid branchId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, string>> GetNameLookupAsync(
        IEnumerable<Guid> warehouseIds,
        CancellationToken cancellationToken = default);
    Task AddAsync(WarehouseAggregate aggregate, CancellationToken cancellationToken = default);
    Task UpdateAsync(WarehouseAggregate aggregate, CancellationToken cancellationToken = default);
}

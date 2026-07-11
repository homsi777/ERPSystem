using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IChinaContainerRepository
{
    Task<ContainerAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ContainerAggregate?> GetByNumberAsync(string containerNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContainerAggregate>> GetListAsync(
        Guid companyId,
        Guid? branchId = null,
        ChinaContainerStatus? status = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, string>> GetNumberLookupAsync(
        Guid companyId,
        IEnumerable<Guid> containerIds,
        CancellationToken cancellationToken = default);
    Task AddAsync(ContainerAggregate aggregate, CancellationToken cancellationToken = default);
    Task UpdateAsync(ContainerAggregate aggregate, CancellationToken cancellationToken = default);
}

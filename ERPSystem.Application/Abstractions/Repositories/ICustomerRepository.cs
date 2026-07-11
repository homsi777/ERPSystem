using ERPSystem.Domain.Aggregates;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface ICustomerRepository
{
    Task<CustomerAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerAggregate>> GetListAsync(
        Guid companyId,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> GetNameLookupAsync(
        Guid companyId,
        IEnumerable<Guid>? customerIds = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CustomerAggregate> Items, int TotalCount)> GetPagedAsync(
        Guid companyId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(CustomerAggregate aggregate, CancellationToken cancellationToken = default);
    Task UpdateAsync(CustomerAggregate aggregate, CancellationToken cancellationToken = default);
}

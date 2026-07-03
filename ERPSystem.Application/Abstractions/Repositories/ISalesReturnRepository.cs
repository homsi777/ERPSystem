using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface ISalesReturnRepository
{
    Task<SalesReturnAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesReturnAggregate>> GetListAsync(
        Guid companyId,
        Guid? branchId = null,
        VoucherStatus? status = null,
        Guid? customerId = null,
        Guid? originalInvoiceId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(SalesReturnAggregate aggregate, CancellationToken cancellationToken = default);
    Task UpdateAsync(SalesReturnAggregate aggregate, CancellationToken cancellationToken = default);
}

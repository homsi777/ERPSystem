using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface ISalesInvoiceRepository
{
    Task<SalesInvoiceAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SalesInvoiceAggregate?> GetByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesInvoiceAggregate>> GetListAsync(
        Guid companyId,
        Guid? branchId = null,
        SalesInvoiceStatus? status = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesInvoiceAggregate>> GetDetailingQueueAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default);
    Task AddAsync(SalesInvoiceAggregate aggregate, CancellationToken cancellationToken = default);
    Task UpdateAsync(SalesInvoiceAggregate aggregate, CancellationToken cancellationToken = default);
}

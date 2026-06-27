using ERPSystem.Domain.Entities.Purchasing;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IPurchaseInvoiceRepository
{
    Task<PurchaseInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseInvoice>> GetListAsync(
        Guid companyId,
        PurchaseInvoiceStatus? status = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default);
    Task UpdateAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default);
}

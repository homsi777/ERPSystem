using ERPSystem.Domain.Entities.Purchasing;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IPurchaseInvoiceRepository
{
    Task<PurchaseInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseInvoice?> GetByNumberAsync(Guid companyId, string invoiceNumber, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<PurchaseInvoice> Items, int TotalCount)> GetPagedAsync(
        Guid companyId,
        string? search = null,
        PurchaseInvoiceStatus? status = null,
        Guid? supplierId = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseInvoice>> GetListAsync(
        Guid companyId,
        PurchaseInvoiceStatus? status = null,
        Guid? supplierId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default);
    Task UpdateAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> GetInvoiceNumberLookupAsync(
        IEnumerable<Guid> invoiceIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchasePayablesAgingAggregate>> GetPayablesAgingAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}

public sealed record PurchasePayablesAgingAggregate(
    Guid SupplierId,
    string SupplierName,
    decimal TotalInvoiced,
    decimal Paid,
    decimal Outstanding,
    DateTime? OldestInvoiceDate);

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseOrder>> GetListAsync(Guid companyId, PurchaseOrderStatus? status = null, CancellationToken cancellationToken = default);
    Task AddAsync(PurchaseOrder order, CancellationToken cancellationToken = default);
    Task UpdateAsync(PurchaseOrder order, CancellationToken cancellationToken = default);
}

public interface IPurchaseReturnRepository
{
    Task<PurchaseReturn?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseReturn>> GetListAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task AddAsync(PurchaseReturn purchaseReturn, CancellationToken cancellationToken = default);
    Task UpdateAsync(PurchaseReturn purchaseReturn, CancellationToken cancellationToken = default);
}

public interface IPurchaseInvoicePaymentRepository
{
    Task AddAsync(PurchaseInvoicePayment payment, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseInvoicePayment>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseInvoicePayment>> GetByVoucherIdAsync(Guid voucherId, CancellationToken cancellationToken = default);
}

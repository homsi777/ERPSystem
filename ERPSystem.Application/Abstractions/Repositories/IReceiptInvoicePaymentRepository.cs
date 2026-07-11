using ERPSystem.Domain.Entities.Sales;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IReceiptInvoicePaymentRepository
{
    Task AddAsync(ReceiptInvoicePayment payment, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceiptInvoicePayment>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceiptInvoicePayment>> GetByVoucherIdAsync(Guid voucherId, CancellationToken cancellationToken = default);
    Task<decimal> GetCollectedTotalAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, decimal>> GetCollectedTotalsAsync(
        IEnumerable<Guid> invoiceIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(ReceiptInvoicePayment Payment, string VoucherNumber)>> GetByInvoiceWithVoucherAsync(
        Guid invoiceId, CancellationToken cancellationToken = default);
}

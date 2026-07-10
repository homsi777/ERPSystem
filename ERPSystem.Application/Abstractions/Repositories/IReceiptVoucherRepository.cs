using ERPSystem.Domain.Entities.Finance;using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IReceiptVoucherRepository
{
    Task<ReceiptVoucher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceiptVoucher>> GetListAsync(
        Guid companyId,
        VoucherStatus? status = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(ReceiptVoucher voucher, CancellationToken cancellationToken = default);
    Task UpdateAsync(ReceiptVoucher voucher, CancellationToken cancellationToken = default);
    Task AddTenderLineAsync(ReceiptTenderLine line, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceiptTenderLine>> GetTenderLinesAsync(Guid voucherId, CancellationToken cancellationToken = default);
    Task<decimal> GetAllocatedTotalAsync(Guid voucherId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<Guid?> GetIdByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task SetIdempotencyKeyAsync(Guid voucherId, string idempotencyKey, CancellationToken cancellationToken = default);
}

public interface IPaymentMethodRepository
{
    Task<IReadOnlyList<PaymentMethod>> GetActiveForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}

public interface IBankAccountRepository
{
    Task<IReadOnlyList<BankAccount>> GetActiveForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

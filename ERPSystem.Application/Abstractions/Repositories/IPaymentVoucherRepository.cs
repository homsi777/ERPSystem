using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IPaymentVoucherRepository
{
    Task<PaymentVoucher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentVoucher>> GetListAsync(
        Guid companyId,
        VoucherStatus? status = null,
        Guid? supplierId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(PaymentVoucher voucher, CancellationToken cancellationToken = default);
    Task UpdateAsync(PaymentVoucher voucher, CancellationToken cancellationToken = default);
}

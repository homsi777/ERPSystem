using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Enums;

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
}

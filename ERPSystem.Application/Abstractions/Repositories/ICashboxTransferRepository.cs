using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface ICashboxTransferRepository
{
    Task<CashboxTransfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CashboxTransfer>> GetListAsync(
        Guid branchId,
        VoucherStatus? status = null,
        Guid? cashboxId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(
        CashboxTransfer transfer,
        Guid companyId,
        Guid branchId,
        CancellationToken cancellationToken = default);
    Task UpdateAsync(CashboxTransfer transfer, CancellationToken cancellationToken = default);
}

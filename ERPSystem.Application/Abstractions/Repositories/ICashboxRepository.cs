using ERPSystem.Domain.Entities.Finance;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface ICashboxRepository
{
    Task<Cashbox?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cashbox>> GetListAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(Guid branchId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<(decimal Receipts, decimal Payments)> GetTodayTotalsAsync(Guid cashboxId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(DateTime Date, string Type, string Number, string Description, decimal Amount, bool IsInbound)>> GetMovementsAsync(
        Guid cashboxId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Cashbox cashbox, CancellationToken cancellationToken = default);
    Task UpdateAsync(Cashbox cashbox, CancellationToken cancellationToken = default);
}

using ERPSystem.Domain.Entities.Finance;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface ICostCenterRepository
{
    Task<IReadOnlyList<CostCenter>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<CostCenter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CostCenter?> GetByCodeAsync(Guid companyId, string code, CancellationToken cancellationToken = default);
    Task AddAsync(CostCenter costCenter, CancellationToken cancellationToken = default);
    Task UpdateAsync(CostCenter costCenter, CancellationToken cancellationToken = default);
}

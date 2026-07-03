using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class CostCenterRepository(ErpDbContext context) : ICostCenterRepository
{
    public async Task<IReadOnlyList<CostCenter>> GetByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var entities = await context.CostCenters.AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.IsActive)
            .OrderBy(c => c.Code)
            .ToListAsync(cancellationToken);
        return entities.Select(ExpenseMapper.ToDomain).ToList();
    }

    public async Task<CostCenter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.CostCenters.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return entity is null ? null : ExpenseMapper.ToDomain(entity);
    }

    public async Task<CostCenter?> GetByCodeAsync(
        Guid companyId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var entity = await context.CostCenters.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Code == code, cancellationToken);
        return entity is null ? null : ExpenseMapper.ToDomain(entity);
    }

    public async Task AddAsync(CostCenter costCenter, CancellationToken cancellationToken = default) =>
        await context.CostCenters.AddAsync(ExpenseMapper.ToEntity(costCenter), cancellationToken);

    public async Task UpdateAsync(CostCenter costCenter, CancellationToken cancellationToken = default)
    {
        var entity = await context.CostCenters.FirstOrDefaultAsync(c => c.Id == costCenter.Id, cancellationToken)
            ?? throw new InvalidOperationException("Cost center not found.");
        entity.Name = costCenter.Name;
        entity.Description = costCenter.Description;
        entity.ParentCostCenterId = costCenter.ParentCostCenterId;
        entity.Status = (int)costCenter.Status;
        entity.IsActive = costCenter.Status == Domain.Enums.CostCenterStatus.Active;
    }
}

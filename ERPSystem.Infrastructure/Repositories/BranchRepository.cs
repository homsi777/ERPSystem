using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Company;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class BranchRepository(ErpDbContext context) : IBranchRepository
{
    public async Task<IReadOnlyList<BranchListItem>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await context.Branches.AsNoTracking()
            .Where(b => b.CompanyId == companyId)
            .OrderBy(b => b.Code)
            .Select(b => new BranchListItem(b.Id, b.CompanyId, b.Code, b.NameAr, b.NameEn))
            .ToListAsync(cancellationToken);
    }

    public async Task<BranchListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Branches.AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BranchListItem(b.Id, b.CompanyId, b.Code, b.NameAr, b.NameEn))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> AddAsync(Guid companyId, string code, string nameAr, string nameEn, CancellationToken cancellationToken = default)
    {
        var entity = new BranchEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = code,
            NameAr = nameAr,
            NameEn = nameEn
        };
        await context.Branches.AddAsync(entity, cancellationToken);
        return entity.Id;
    }

    public async Task UpdateAsync(Guid id, string code, string nameAr, string nameEn, CancellationToken cancellationToken = default)
    {
        var entity = await context.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Branch not found.");
        entity.Code = code;
        entity.NameAr = nameAr;
        entity.NameEn = nameEn;
    }
}

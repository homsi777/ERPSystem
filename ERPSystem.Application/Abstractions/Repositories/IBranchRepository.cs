namespace ERPSystem.Application.Abstractions.Repositories;

public sealed record BranchListItem(Guid Id, Guid CompanyId, string Code, string NameAr, string NameEn);

public interface IBranchRepository
{
    Task<IReadOnlyList<BranchListItem>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<BranchListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(Guid companyId, string code, string nameAr, string nameEn, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, string code, string nameAr, string nameEn, CancellationToken cancellationToken = default);
}

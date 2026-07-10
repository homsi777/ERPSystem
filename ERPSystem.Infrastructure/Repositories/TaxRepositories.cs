using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Domain.Entities.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class TaxCodeRepository(ErpDbContext context) : ITaxCodeRepository
{
    public async Task<TaxCode?> GetByIdAsync(Guid companyId, Guid taxCodeId, CancellationToken cancellationToken = default)
    {
        var entity = await context.TaxCodes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.CompanyId == companyId && t.Id == taxCodeId && t.IsActive, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<TaxCode>> GetActiveForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var list = await context.TaxCodes.AsNoTracking()
            .Where(t => t.CompanyId == companyId && t.IsActive)
            .ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    private static TaxCode Map(TaxCodeEntity e) =>
        TaxCode.FromPersistence(
            e.Id,
            e.CompanyId,
            e.Code,
            e.Name,
            e.Rate,
            (TaxPriceMode)e.PriceMode,
            (TaxCategory)e.Category,
            e.SalesTaxAccountId,
            e.EffectiveFrom,
            e.EffectiveTo,
            e.IsActive);
}

internal sealed class SalesPostingProfileRepository(ErpDbContext context) : ISalesPostingProfileRepository
{
    public async Task<SalesPostingProfile?> GetForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var entity = await context.SalesPostingProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.IsActive, cancellationToken);
        return entity is null ? null : new SalesPostingProfile
        {
            CompanyId = entity.CompanyId,
            AccountsReceivableAccountId = entity.AccountsReceivableAccountId,
            SalesRevenueAccountId = entity.SalesRevenueAccountId,
            SalesDiscountAccountId = entity.SalesDiscountAccountId,
            VatPayableAccountId = entity.VatPayableAccountId,
            InventoryAccountId = entity.InventoryAccountId,
            CogsAccountId = entity.CogsAccountId,
            RoundingAccountId = entity.RoundingAccountId
        };
    }
}

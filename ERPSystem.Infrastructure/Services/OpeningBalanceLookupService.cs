using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Services;

internal sealed class OpeningBalanceLookupService(ErpDbContext context) : IOpeningBalanceLookupService
{
    public async Task<OpeningBalanceLookupsDto> GetLookupsAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var branchIds = await context.Branches.AsNoTracking()
            .Where(b => b.CompanyId == companyId)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var customers = await context.Customers.AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.NameAr)
            .Select(c => new OpeningBalanceLookupItemDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.NameAr,
                Extra = c.OpeningBalancePosted ? "posted" : null
            })
            .ToListAsync(cancellationToken);

        var suppliers = await context.Suppliers.AsNoTracking()
            .Where(s => s.CompanyId == companyId)
            .OrderBy(s => s.NameAr)
            .Select(s => new OpeningBalanceLookupItemDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.NameAr,
                Extra = s.OpeningBalancePosted ? "posted" : null
            })
            .ToListAsync(cancellationToken);

        var partners = await context.CapitalPartners.AsNoTracking()
            .Where(p => p.CompanyId == companyId && p.IsActive)
            .OrderBy(p => p.FullName)
            .Select(p => new OpeningBalanceLookupItemDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.FullName
            })
            .ToListAsync(cancellationToken);

        List<OpeningBalanceLookupItemDto> cashboxes;
        List<OpeningBalanceLookupItemDto> warehouses;
        if (branchIds.Count == 0)
        {
            cashboxes = [];
            warehouses = [];
        }
        else
        {
            cashboxes = await context.Cashboxes.AsNoTracking()
                .Where(c => branchIds.Contains(c.BranchId) && c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new OpeningBalanceLookupItemDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Extra = c.Currency
                })
                .ToListAsync(cancellationToken);

            warehouses = await context.Warehouses.AsNoTracking()
                .Where(w => branchIds.Contains(w.BranchId) && w.IsActive)
                .OrderBy(w => w.NameAr)
                .Select(w => new OpeningBalanceLookupItemDto
                {
                    Id = w.Id,
                    Code = w.Code,
                    Name = w.NameAr
                })
                .ToListAsync(cancellationToken);
        }

        var accounts = await context.Accounts.AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.IsActive && a.IsPostable)
            .OrderBy(a => a.Code)
            .Select(a => new OpeningBalanceLookupItemDto
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.NameAr,
                Extra = a.AccountType
            })
            .ToListAsync(cancellationToken);

        return new OpeningBalanceLookupsDto
        {
            Customers = customers,
            Suppliers = suppliers,
            Partners = partners,
            Cashboxes = cashboxes,
            Warehouses = warehouses,
            Accounts = accounts
        };
    }
}

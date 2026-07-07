using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Domain.Entities.Accounting;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Mapping;
using ERPSystem.Infrastructure.Persistence.Models.Accounting;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class AccountRepository(ErpDbContext context, ICacheService cache) : IAccountRepository
{
    private const string AccountCachePrefix = "lookup:accounts:";

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<Account?> GetByCodeAsync(Guid companyId, string code, CancellationToken cancellationToken = default)
    {
        var entity = await context.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Code == code, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<Account>> GetListAsync(
        Guid companyId,
        string? search = null,
        GlAccountType? accountType = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{AccountCachePrefix}{companyId}:type:{accountType?.ToString() ?? "all"}:active:{activeOnly}";
        if (string.IsNullOrWhiteSpace(search))
        {
            var cached = await cache.GetAsync<IReadOnlyList<Account>>(cacheKey, cancellationToken);
            if (cached is not null)
                return cached;
        }

        var query = context.Accounts.AsNoTracking().Where(a => a.CompanyId == companyId);
        if (activeOnly)
            query = query.Where(a => a.IsActive);
        if (accountType.HasValue)
            query = query.Where(a => a.AccountType == accountType.Value.ToString());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a =>
                a.Code.Contains(term) ||
                a.NameAr.Contains(term) ||
                a.NameEn.Contains(term));
        }

        var entities = await query.OrderBy(a => a.Code).ToListAsync(cancellationToken);
        var accounts = entities.Select(ToDomain).ToList();
        if (string.IsNullOrWhiteSpace(search))
            await cache.SetAsync(cacheKey, accounts, TimeSpan.FromMinutes(30), cancellationToken);
        return accounts;
    }

    public Task<bool> HasChildrenAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        context.Accounts.AsNoTracking().AnyAsync(a => a.ParentId == accountId, cancellationToken);

    public Task<bool> HasJournalLinesAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        context.JournalEntryLines.AsNoTracking().AnyAsync(l => l.AccountId == accountId, cancellationToken);

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        await context.Accounts.AddAsync(new AccountEntity
        {
            Id = account.Id,
            CompanyId = account.CompanyId,
            Code = account.Code,
            NameAr = account.NameAr,
            NameEn = account.NameEn,
            AccountType = account.AccountType.ToString(),
            ParentId = account.ParentId,
            IsPostable = account.IsPostable,
            IsActive = account.IsActive
        }, cancellationToken);
        await cache.RemoveByPrefixAsync(AccountCachePrefix, cancellationToken);
    }

    public async Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        var entity = await context.Accounts.FirstOrDefaultAsync(a => a.Id == account.Id, cancellationToken)
            ?? throw new InvalidOperationException("Account not found.");

        entity.Code = account.Code;
        entity.NameAr = account.NameAr;
        entity.NameEn = account.NameEn;
        entity.AccountType = account.AccountType.ToString();
        entity.ParentId = account.ParentId;
        entity.IsPostable = account.IsPostable;
        entity.IsActive = account.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await cache.RemoveByPrefixAsync(AccountCachePrefix, cancellationToken);
    }

    private static Account ToDomain(AccountEntity entity)
    {
        var account = DomainHydrator.Create<Account>();
        DomainHydrator.Set(account, nameof(Account.Id), entity.Id);
        DomainHydrator.Set(account, nameof(Account.CompanyId), entity.CompanyId);
        DomainHydrator.Set(account, nameof(Account.Code), entity.Code);
        DomainHydrator.Set(account, nameof(Account.NameAr), entity.NameAr);
        DomainHydrator.Set(account, nameof(Account.NameEn), entity.NameEn);
        DomainHydrator.Set(account, nameof(Account.AccountType), AccountingDisplayExtensions.ParseAccountType(entity.AccountType));
        DomainHydrator.Set(account, nameof(Account.ParentId), entity.ParentId);
        DomainHydrator.Set(account, nameof(Account.IsPostable), entity.IsPostable);
        DomainHydrator.Set(account, nameof(Account.IsActive), entity.IsActive);
        return account;
    }
}

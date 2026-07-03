using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Capital;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Mapping;
using ERPSystem.Infrastructure.Persistence.Models.Capital;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class CapitalPartnerRepository(ErpDbContext context) : ICapitalPartnerRepository
{
    public async Task<CapitalPartnerAggregate?> GetByIdAsync(
        Guid id,
        bool includeChildren = false,
        CancellationToken cancellationToken = default)
    {
        var bundle = await GetWithAuditAsync(id, includeChildren, cancellationToken);
        return bundle?.Aggregate;
    }

    public async Task<CapitalPartnerWithAudit?> GetWithAuditAsync(
        Guid id,
        bool includeChildren = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<CapitalPartnerEntity> query = context.CapitalPartners.AsNoTracking();
        if (includeChildren)
            query = query
                .Include(p => p.Participations)
                .Include(p => p.BankAccounts)
                .Include(p => p.Transactions);

        var entity = await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity is null)
            return null;

        string? createdByName = null;
        if (entity.CreatedByUserId is Guid userId)
        {
            createdByName = await context.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.FullNameAr)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new CapitalPartnerWithAudit(
            CapitalMapper.ToAggregate(entity),
            entity.CreatedAt,
            createdByName,
            entity.UpdatedAt);
    }

    public async Task<(IReadOnlyList<CapitalPartnerAggregate> Items, int TotalCount)> GetPagedAsync(
        Guid companyId,
        CapitalPartnerListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.CapitalPartners.AsNoTracking()
            .Include(p => p.Participations)
            .Include(p => p.Transactions)
            .Where(p => p.CompanyId == companyId);

        if (!filter.IncludeArchived)
            query = query.Where(p => p.Status != (int)PartnerStatus.Archived);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(p =>
                p.Code.Contains(term) ||
                p.FullName.Contains(term) ||
                (p.Phone != null && p.Phone.Contains(term)) ||
                (p.NationalId != null && p.NationalId.Contains(term)));
        }

        if (filter.Status is PartnerStatus status)
            query = query.Where(p => p.Status == (int)status);

        if (filter.Scope is PartnershipScope scope)
            query = query.Where(p => p.Participations.Any(x => x.Scope == (int)scope && x.IsActive));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.Select(CapitalMapper.ToAggregate).ToList(), total);
    }

    public async Task AddAsync(CapitalPartnerAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var entity = CapitalMapper.ToEntity(aggregate);
        await context.CapitalPartners.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(CapitalPartnerAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var entity = await context.CapitalPartners
            .Include(p => p.Participations)
            .Include(p => p.BankAccounts)
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == aggregate.Id, cancellationToken);

        if (entity is null)
            throw new InvalidOperationException($"Partner {aggregate.Id} not found.");

        CapitalMapper.UpdateEntity(entity, aggregate);
        entity.UpdatedAt = DateTime.UtcNow;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.CapitalPartners.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity is not null)
            context.CapitalPartners.Remove(entity);
    }

    public async Task AddAuditEntryAsync(PartnerAuditEntry entry, CancellationToken cancellationToken = default) =>
        await context.PartnerAuditLogs.AddAsync(CapitalMapper.ToAuditEntity(entry), cancellationToken);

    public async Task<IReadOnlyList<PartnerAuditEntry>> GetAuditTrailAsync(
        Guid partnerId,
        CancellationToken cancellationToken = default)
    {
        var entries = await context.PartnerAuditLogs.AsNoTracking()
            .Where(e => e.PartnerId == partnerId)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(cancellationToken);
        return entries.Select(CapitalMapper.ToAuditDomain).ToList();
    }

    public async Task AddTimelineEventAsync(PartnerTimelineEvent entry, CancellationToken cancellationToken = default) =>
        await context.PartnerTimelineEvents.AddAsync(CapitalMapper.ToTimelineEntity(entry), cancellationToken);

    public async Task<IReadOnlyList<PartnerTimelineEvent>> GetTimelineAsync(
        Guid partnerId,
        CancellationToken cancellationToken = default)
    {
        var entries = await context.PartnerTimelineEvents.AsNoTracking()
            .Where(e => e.PartnerId == partnerId)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(cancellationToken);
        return entries.Select(CapitalMapper.ToTimelineDomain).ToList();
    }

    public async Task<CapitalDashboardData> GetDashboardDataAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var partners = await context.CapitalPartners.AsNoTracking()
            .Include(p => p.Participations)
            .Include(p => p.Transactions)
            .Where(p => p.CompanyId == companyId && p.Status != (int)PartnerStatus.Archived)
            .ToListAsync(cancellationToken);

        var aggregates = partners.Select(CapitalMapper.ToAggregate).ToList();
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalCapital = aggregates.Sum(a => a.Partner.CurrentCapitalBase);
        var activePartners = aggregates.Count(a => a.Partner.Status == PartnerStatus.Active);
        var activeParticipations = aggregates.Sum(a => a.Partner.Participations.Count(p => p.IsActive));

        var monthlyDistributed = aggregates
            .SelectMany(a => a.Partner.Transactions)
            .Where(t => t.Type == CapitalTransactionType.ProfitDistribution
                && t.ApprovalStatus == CapitalApprovalStatus.Approved
                && t.TransactionDate >= monthStart)
            .Sum(t => t.AmountBase);

        var pendingDistributions = await context.ProfitDistributions.AsNoTracking()
            .Where(d => d.CompanyId == companyId
                && (d.Status == (int)DistributionStatus.PendingApproval || d.Status == (int)DistributionStatus.Calculated))
            .ToListAsync(cancellationToken);

        var pendingSettlements = pendingDistributions.Sum(d => d.NetProfit > 0 ? d.NetProfit : d.NetLoss);

        var topInvestor = aggregates
            .OrderByDescending(a => a.Partner.CurrentCapitalBase)
            .FirstOrDefault();

        var scopeBreakdown = aggregates
            .SelectMany(a => a.Partner.Transactions
                .Where(t => t.ApprovalStatus == CapitalApprovalStatus.Approved)
                .Select(t => new { t.Scope, t.SignedBaseAmount }))
            .GroupBy(x => x.Scope)
            .Select(g => new CapitalScopeBreakdownPoint
            {
                Scope = g.Key,
                AmountBase = g.Sum(x => x.SignedBaseAmount)
            })
            .ToList();

        var currencyBreakdown = aggregates
            .SelectMany(a => a.Partner.Transactions
                .Where(t => t.ApprovalStatus == CapitalApprovalStatus.Approved)
                .Select(t => new { t.Currency, t.AmountOriginal, t.AmountBase }))
            .GroupBy(x => x.Currency)
            .Select(g => new CapitalCurrencyBreakdownPoint
            {
                Currency = g.Key,
                AmountOriginal = g.Sum(x => x.AmountOriginal),
                AmountBase = g.Sum(x => x.AmountBase)
            })
            .ToList();

        var investmentTrend = aggregates
            .SelectMany(a => a.Partner.Transactions
                .Where(t => t.SignedBaseAmount > 0 && t.ApprovalStatus == CapitalApprovalStatus.Approved)
                .Select(t => new { t.TransactionDate.Year, t.TransactionDate.Month, t.AmountBase }))
            .GroupBy(x => new { x.Year, x.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Take(12)
            .Select(g => new CapitalMonthlyTrendPoint
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                AmountBase = g.Sum(x => x.AmountBase)
            })
            .ToList();

        var topInvestors = aggregates
            .OrderByDescending(a => a.Partner.CurrentCapitalBase)
            .Take(5)
            .Select(a => new CapitalTopInvestorPoint
            {
                PartnerId = a.Partner.Id,
                PartnerName = a.Partner.FullName,
                CapitalBase = a.Partner.CurrentCapitalBase
            })
            .ToList();

        var pendingPoints = pendingDistributions
            .Select(d => new CapitalPendingDistributionPoint
            {
                DistributionId = d.Id,
                Code = d.Code,
                Status = (DistributionStatus)d.Status,
                NetAmount = d.NetProfit > 0 ? d.NetProfit : d.NetLoss
            })
            .ToList();

        return new CapitalDashboardData
        {
            TotalCapitalBase = totalCapital,
            ActivePartnersCount = activePartners,
            ActiveParticipationsCount = activeParticipations,
            MonthlyDistributedProfit = monthlyDistributed,
            PendingSettlementsBase = pendingSettlements,
            LargestInvestorName = topInvestor?.Partner.FullName ?? "",
            LargestInvestorBase = topInvestor?.Partner.CurrentCapitalBase ?? 0,
            ScopeBreakdown = scopeBreakdown,
            CurrencyBreakdown = currencyBreakdown,
            InvestmentTrend = investmentTrend,
            TopInvestors = topInvestors,
            PendingDistributions = pendingPoints
        };
    }

    public async Task<ProfitDistribution?> GetDistributionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.ProfitDistributions.AsNoTracking()
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        return entity is null ? null : CapitalMapper.ToDistributionDomain(entity);
    }

    public async Task AddDistributionAsync(ProfitDistribution distribution, CancellationToken cancellationToken = default) =>
        await context.ProfitDistributions.AddAsync(CapitalMapper.ToDistributionEntity(distribution), cancellationToken);

    public async Task UpdateDistributionAsync(ProfitDistribution distribution, CancellationToken cancellationToken = default)
    {
        var entity = await context.ProfitDistributions
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == distribution.Id, cancellationToken);
        if (entity is null)
            throw new InvalidOperationException($"Distribution {distribution.Id} not found.");
        CapitalMapper.UpdateDistributionEntity(entity, distribution);
        entity.UpdatedAt = DateTime.UtcNow;
    }

    public async Task<IReadOnlyList<ProfitDistribution>> GetDistributionsAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var entities = await context.ProfitDistributions.AsNoTracking()
            .Include(d => d.Lines)
            .Where(d => d.CompanyId == companyId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(CapitalMapper.ToDistributionDomain).ToList();
    }

    public async Task<(IReadOnlyList<CapitalTransactionRow> Items, int TotalCount)> GetTransactionsPagedAsync(
        Guid companyId,
        CapitalTransactionListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.CapitalTransactions.AsNoTracking()
            .Include(t => t.Partner)
            .Where(t => t.Partner!.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(t =>
                (t.Notes != null && t.Notes.Contains(term)) ||
                t.Partner!.FullName.Contains(term) ||
                t.Partner.Code.Contains(term));
        }

        if (filter.PartnerId is Guid partnerId)
            query = query.Where(t => t.PartnerId == partnerId);

        if (filter.Type is CapitalTransactionType type)
            query = query.Where(t => t.Type == (int)type);

        if (filter.FromDate is DateTime from)
            query = query.Where(t => t.TransactionDate >= UtcDateTimeNormalizer.ToUtc(from));

        if (filter.ToDate is DateTime to)
            query = query.Where(t => t.TransactionDate <= UtcDateTimeNormalizer.ToUtc(to));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var rows = items.Select(t =>
        {
            var type = (CapitalTransactionType)t.Type;
            var isOutflow = type is CapitalTransactionType.PartialWithdrawal
                or CapitalTransactionType.FullWithdrawal
                or CapitalTransactionType.LossDistribution;
            return new CapitalTransactionRow
            {
                Id = t.Id,
                PartnerId = t.PartnerId,
                PartnerCode = t.Partner!.Code,
                PartnerName = t.Partner.FullName,
                Type = type,
                AmountOriginal = t.AmountOriginal,
                Currency = t.Currency,
                SignedBaseAmount = isOutflow ? -t.AmountBase : t.AmountBase,
                TransactionDate = t.TransactionDate,
                Notes = t.Notes
            };
        }).ToList();

        return (rows, total);
    }
}

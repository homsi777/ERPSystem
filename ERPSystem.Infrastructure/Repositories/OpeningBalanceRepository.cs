using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.Queries.Finance;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Mapping;
using ERPSystem.Infrastructure.Persistence.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class OpeningBalanceRepository(ErpDbContext context) : IOpeningBalanceRepository
{
    public async Task AddAsync(OpeningBalanceDocument document, CancellationToken cancellationToken = default)
    {
        var entity = OpeningBalanceMapper.ToEntity(document, document.CreatedByUserId);
        await context.OpeningBalanceDocuments.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(OpeningBalanceDocument document, CancellationToken cancellationToken = default)
    {
        var existing = await context.OpeningBalanceDocuments
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == document.Id, cancellationToken)
            ?? throw new InvalidOperationException("Opening balance document not found.");

        context.OpeningBalanceLines.RemoveRange(existing.Lines);
        var entity = OpeningBalanceMapper.ToEntity(document, null);
        context.Entry(existing).CurrentValues.SetValues(entity);
        existing.Lines = entity.Lines;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    public async Task<OpeningBalanceDocument?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.OpeningBalanceDocuments.AsNoTracking()
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        return entity is null ? null : OpeningBalanceMapper.ToAggregate(entity);
    }

    public async Task<(IReadOnlyList<OpeningBalanceDocument> Items, int TotalCount)> GetPagedAsync(
        Guid companyId,
        OpeningBalanceListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.OpeningBalanceDocuments.AsNoTracking()
            .Include(d => d.Lines)
            .Where(d => d.CompanyId == companyId);

        if (filter.Type is { } type)
            query = query.Where(d => d.Type == (int)type);
        if (filter.Status is { } status)
            query = query.Where(d => d.Status == (int)status);
        if (!filter.IncludeArchived)
            query = query.Where(d => d.Status != (int)OpeningBalanceStatus.Archived);
        if (filter.From is { } from)
            query = query.Where(d => d.OpeningDate >= ApplicationDateNormalizer.ToUtcDate(from));
        if (filter.To is { } to)
        {
            var toExclusive = ApplicationDateNormalizer.ToUtcDate(to).AddDays(1);
            query = query.Where(d => d.OpeningDate < toExclusive);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(d =>
                EF.Functions.ILike(d.Number, $"%{s}%") ||
                (d.Reference != null && EF.Functions.ILike(d.Reference, $"%{s}%")) ||
                (d.Description != null && EF.Functions.ILike(d.Description, $"%{s}%")) ||
                (d.Notes != null && EF.Functions.ILike(d.Notes, $"%{s}%")) ||
                d.Lines.Any(l => l.PartyName != null && EF.Functions.ILike(l.PartyName, $"%{s}%")));
        }

        if (filter.PartyId is Guid partyId)
            query = query.Where(d => d.Lines.Any(l => l.PartyId == partyId));

        if (!string.IsNullOrWhiteSpace(filter.PartySearch))
        {
            var ps = filter.PartySearch.Trim();
            query = query.Where(d => d.Lines.Any(l =>
                l.PartyName != null && EF.Functions.ILike(l.PartyName, $"%{ps}%")));
        }

        if (filter.AmountFrom is decimal amountFrom)
            query = query.Where(d => d.TotalBaseAmount >= amountFrom);

        if (filter.AmountTo is decimal amountTo)
            query = query.Where(d => d.TotalBaseAmount <= amountTo);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.Select(OpeningBalanceMapper.ToAggregate).ToList(), total);
    }

    public async Task<IReadOnlyList<OpeningBalanceDocument>> GetAllAsync(
        Guid companyId,
        OpeningBalanceType? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.OpeningBalanceDocuments.AsNoTracking()
            .Include(d => d.Lines)
            .Where(d => d.CompanyId == companyId && d.Status != (int)OpeningBalanceStatus.Archived);
        if (type is { } t)
            query = query.Where(d => d.Type == (int)t);
        var items = await query.OrderByDescending(d => d.CreatedAt).ToListAsync(cancellationToken);
        return items.Select(OpeningBalanceMapper.ToAggregate).ToList();
    }

    public async Task AddEventAsync(OpeningBalanceEvent evt, CancellationToken cancellationToken = default)
    {
        await context.OpeningBalanceEvents.AddAsync(new OpeningBalanceEventEntity
        {
            Id = evt.Id,
            DocumentId = evt.DocumentId,
            OccurredAt = evt.OccurredAt,
            UserId = evt.UserId,
            UserName = evt.UserName,
            Action = evt.Action,
            OldValues = evt.OldValues,
            NewValues = evt.NewValues,
            Notes = evt.Notes,
            MachineName = evt.MachineName,
            IpAddress = evt.IpAddress
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<OpeningBalanceEvent>> GetEventsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var events = await context.OpeningBalanceEvents.AsNoTracking()
            .Where(e => e.DocumentId == documentId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(cancellationToken);
        return events.Select(OpeningBalanceMapper.ToEventAggregate).ToList();
    }

    public async Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var events = await context.OpeningBalanceEvents
            .Where(e => e.DocumentId == documentId)
            .ToListAsync(cancellationToken);
        if (events.Count > 0)
            context.OpeningBalanceEvents.RemoveRange(events);

        var doc = await context.OpeningBalanceDocuments
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken)
            ?? throw new InvalidOperationException("Opening balance document not found.");

        context.OpeningBalanceLines.RemoveRange(doc.Lines);
        context.OpeningBalanceDocuments.Remove(doc);
    }

    public async Task<IReadOnlySet<string>> GetExistingLineKeysAsync(
        Guid companyId,
        OpeningBalanceType type,
        Guid? excludeDocumentId,
        CancellationToken cancellationToken = default)
    {
        var postedStatuses = new[]
        {
            (int)OpeningBalanceStatus.Posted,
            (int)OpeningBalanceStatus.Locked,
            (int)OpeningBalanceStatus.Approved,
            (int)OpeningBalanceStatus.PendingApproval
        };

        var query = from l in context.OpeningBalanceLines.AsNoTracking()
                    join d in context.OpeningBalanceDocuments.AsNoTracking() on l.DocumentId equals d.Id
                    where d.CompanyId == companyId
                          && d.Type == (int)type
                          && postedStatuses.Contains(d.Status)
                    select l;

        if (excludeDocumentId is Guid ex)
            query = query.Where(l => l.DocumentId != ex);

        var lines = await query.Select(l => l).ToListAsync(cancellationToken);
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var le in lines)
        {
            var agg = OpeningBalanceMapper.ToLineAggregate(le);
            keys.Add(OpeningBalanceMapper.BuildLineKey(type, agg));
        }

        return keys;
    }

    public async Task<IReadOnlyList<OpeningBalanceJournalLineDto>> GetJournalLinesAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from l in context.JournalEntryLines.AsNoTracking()
            join j in context.JournalEntries.AsNoTracking() on l.JournalEntryId equals j.Id
            join a in context.Accounts.AsNoTracking() on l.AccountId equals a.Id
            where j.SourceType == (int)OpeningBalanceDocumentTypePolicy.SourceType && j.SourceId == documentId
            orderby j.EntryDate
            select new OpeningBalanceJournalLineDto
            {
                EntryNumber = j.EntryNumber,
                EntryDate = j.EntryDate,
                AccountCode = a.Code,
                AccountName = a.NameAr,
                Debit = l.Debit,
                Credit = l.Credit,
                Narrative = l.Narrative
            }).ToListAsync(cancellationToken);
    }

    public async Task<CustomerOpeningBalanceSummaryDto> GetSummaryAsync(
        Guid companyId,
        OpeningBalanceListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = context.OpeningBalanceDocuments.AsNoTracking()
            .Where(d => d.CompanyId == companyId);

        if (filter.Type is { } type)
            query = query.Where(d => d.Type == (int)type);
        if (filter.Status is { } status)
            query = query.Where(d => d.Status == (int)status);
        if (!filter.IncludeArchived)
            query = query.Where(d => d.Status != (int)OpeningBalanceStatus.Archived);
        if (filter.From is { } from)
            query = query.Where(d => d.OpeningDate >= ApplicationDateNormalizer.ToUtcDate(from));
        if (filter.To is { } to)
        {
            var toExclusive = ApplicationDateNormalizer.ToUtcDate(to).AddDays(1);
            query = query.Where(d => d.OpeningDate < toExclusive);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(d =>
                EF.Functions.ILike(d.Number, $"%{s}%") ||
                (d.Reference != null && EF.Functions.ILike(d.Reference, $"%{s}%")) ||
                (d.Description != null && EF.Functions.ILike(d.Description, $"%{s}%")) ||
                (d.Notes != null && EF.Functions.ILike(d.Notes, $"%{s}%")) ||
                context.OpeningBalanceLines.Any(l =>
                    l.DocumentId == d.Id &&
                    l.PartyName != null &&
                    EF.Functions.ILike(l.PartyName, $"%{s}%")));
        }

        if (filter.PartyId is Guid partyId)
            query = query.Where(d => context.OpeningBalanceLines.Any(l =>
                l.DocumentId == d.Id && l.PartyId == partyId));

        if (!string.IsNullOrWhiteSpace(filter.PartySearch))
        {
            var ps = filter.PartySearch.Trim();
            query = query.Where(d => context.OpeningBalanceLines.Any(l =>
                l.DocumentId == d.Id &&
                l.PartyName != null &&
                EF.Functions.ILike(l.PartyName, $"%{ps}%")));
        }

        if (filter.AmountFrom is decimal amountFrom)
            query = query.Where(d => d.TotalBaseAmount >= amountFrom);
        if (filter.AmountTo is decimal amountTo)
            query = query.Where(d => d.TotalBaseAmount <= amountTo);

        var docs = await query.ToListAsync(cancellationToken);
        return new CustomerOpeningBalanceSummaryDto
        {
            TotalCount = docs.Count,
            TotalDebit = docs.Sum(d => d.TotalDebit),
            TotalCredit = docs.Sum(d => d.TotalCredit),
            NetBalance = docs.Sum(d => d.TotalDebit - d.TotalCredit),
            PendingApprovalCount = docs.Count(d =>
                d.Status == (int)OpeningBalanceStatus.PendingApproval ||
                d.Status == (int)OpeningBalanceStatus.Draft),
            PostedCount = docs.Count(d =>
                d.Status == (int)OpeningBalanceStatus.Posted ||
                d.Status == (int)OpeningBalanceStatus.Locked)
        };
    }
}

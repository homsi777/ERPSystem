using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Application.Queries.Accounting;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.UseCases.Accounting;

public sealed class GetJournalEntryListHandler(IJournalEntryRepository journalEntryRepository)
{
    public async Task<ApplicationResult<PagedResult<JournalEntryListDto>>> HandleAsync(
        GetJournalEntryListQuery query,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await journalEntryRepository.GetPagedAsync(
            query.CompanyId,
            query.Filter,
            query.Page,
            query.PageSize,
            cancellationToken);

        var dtos = items.Select(r => new JournalEntryListDto
        {
            Id = r.Id,
            EntryNumber = r.EntryNumber,
            EntryDate = r.EntryDate,
            Description = r.Description,
            Status = r.Status,
            StatusDisplay = r.Status.ToDisplay(),
            DebitTotal = r.DebitTotal,
            CreditTotal = r.CreditTotal,
            LineCount = r.LineCount,
            SourceType = r.SourceType,
            SourceTypeDisplay = r.SourceType.ToDisplay()
        }).ToList();

        return ApplicationResult<PagedResult<JournalEntryListDto>>.Success(new PagedResult<JournalEntryListDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }
}

public sealed class GetJournalEntryDetailsHandler(
    IJournalEntryRepository journalEntryRepository,
    IAccountRepository accountRepository)
{
    public async Task<ApplicationResult<JournalEntryDetailsDto>> HandleAsync(
        GetJournalEntryDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var entry = await journalEntryRepository.GetByIdAsync(query.EntryId, cancellationToken);
        if (entry is null)
            return ApplicationResult<JournalEntryDetailsDto>.NotFound("Journal entry not found.");

        var accountMap = new Dictionary<Guid, (string Code, string Name)>();
        foreach (var id in entry.Lines.Select(l => l.AccountId).Distinct())
        {
            var acc = await accountRepository.GetByIdAsync(id, cancellationToken);
            if (acc is not null)
                accountMap[id] = (acc.Code, acc.NameAr);
        }

        return ApplicationResult<JournalEntryDetailsDto>.Success(new JournalEntryDetailsDto
        {
            Id = entry.Id,
            EntryNumber = entry.EntryNumber,
            EntryDate = entry.EntryDate,
            Description = entry.Description,
            Status = entry.Status,
            StatusDisplay = entry.Status.ToDisplay(),
            DebitTotal = entry.DebitTotal.Amount,
            CreditTotal = entry.CreditTotal.Amount,
            SourceType = entry.SourceType,
            SourceTypeDisplay = entry.SourceType.ToDisplay(),
            SourceId = entry.SourceId,
            PostedAt = entry.PostedAt,
            Lines = entry.Lines.Select(l => new JournalEntryLineDetailsDto
            {
                Id = l.Id,
                AccountId = l.AccountId,
                AccountCode = accountMap.GetValueOrDefault(l.AccountId).Code,
                AccountName = accountMap.GetValueOrDefault(l.AccountId).Name,
                Debit = l.Debit.Amount,
                Credit = l.Credit.Amount,
                Narrative = l.Narrative
            }).ToList()
        });
    }
}

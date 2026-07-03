using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Application.Queries.Accounting;
using ERPSystem.Application.Results;

namespace ERPSystem.Application.UseCases.Accounting;

public sealed class GetJournalBooksHandler(IJournalBookRepository journalBookRepository)
{
    public async Task<ApplicationResult<IReadOnlyList<JournalBookListDto>>> HandleAsync(
        GetJournalBooksQuery query,
        CancellationToken cancellationToken = default)
    {
        var books = await journalBookRepository.GetListAsync(query.CompanyId, cancellationToken);
        var dtos = books.Select(b => new JournalBookListDto
        {
            Id = b.Id,
            Code = b.Code,
            NameAr = b.NameAr,
            BookType = b.BookType,
            BookTypeDisplay = b.BookType.ToDisplay()
        }).ToList();

        return ApplicationResult<IReadOnlyList<JournalBookListDto>>.Success(dtos);
    }
}

public sealed class GetTrialBalanceHandler(IAccountingReportRepository reportRepository)
{
    public async Task<ApplicationResult<IReadOnlyList<TrialBalanceLineDto>>> HandleAsync(
        GetTrialBalanceQuery query,
        CancellationToken cancellationToken = default)
    {
        var rows = await reportRepository.GetTrialBalanceAsync(
            query.CompanyId, query.AsOfDate, cancellationToken);

        var dtos = rows.Select(r => new TrialBalanceLineDto
        {
            AccountId = r.AccountId,
            AccountCode = r.AccountCode,
            AccountName = r.AccountName,
            AccountTypeDisplay = r.AccountType.ToDisplay(),
            DebitTotal = r.DebitTotal,
            CreditTotal = r.CreditTotal,
            Balance = r.Balance
        }).ToList();

        return ApplicationResult<IReadOnlyList<TrialBalanceLineDto>>.Success(dtos);
    }
}

public sealed class GetAccountLedgerHandler(IAccountingReportRepository reportRepository)
{
    public async Task<ApplicationResult<IReadOnlyList<AccountLedgerLineDto>>> HandleAsync(
        GetAccountLedgerQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.AccountId == Guid.Empty)
            return ApplicationResult<IReadOnlyList<AccountLedgerLineDto>>.ValidationFailed(
                nameof(query.AccountId), "Account is required.");

        var rows = await reportRepository.GetAccountLedgerAsync(
            query.AccountId, query.FromDate, query.ToDate, cancellationToken);

        var dtos = rows.Select(r => new AccountLedgerLineDto
        {
            JournalEntryId = r.JournalEntryId,
            EntryNumber = r.EntryNumber,
            EntryDate = r.EntryDate,
            Description = r.Description,
            LineNarrative = r.LineNarrative,
            Debit = r.Debit,
            Credit = r.Credit,
            RunningBalance = r.RunningBalance
        }).ToList();

        return ApplicationResult<IReadOnlyList<AccountLedgerLineDto>>.Success(dtos);
    }
}

using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Common;
using ERPSystem.Domain.Entities.Accounting;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Mapping;
using ERPSystem.Infrastructure.Persistence.Models.Accounting;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class JournalBookRepository(ErpDbContext context) : IJournalBookRepository
{
    public async Task<IReadOnlyList<JournalBook>> GetListAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var entities = await context.JournalBooks.AsNoTracking()
            .Where(b => b.CompanyId == companyId && b.IsActive)
            .OrderBy(b => b.Code)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<JournalBook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.JournalBooks.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    private static JournalBook ToDomain(JournalBookEntity entity)
    {
        var book = DomainHydrator.Create<JournalBook>();
        DomainHydrator.Set(book, nameof(JournalBook.Id), entity.Id);
        DomainHydrator.Set(book, nameof(JournalBook.CompanyId), entity.CompanyId);
        DomainHydrator.Set(book, nameof(JournalBook.Code), entity.Code);
        DomainHydrator.Set(book, nameof(JournalBook.NameAr), entity.NameAr);
        DomainHydrator.Set(book, nameof(JournalBook.NameEn), entity.NameEn);
        DomainHydrator.Set(book, nameof(JournalBook.BookType), (JournalBookType)entity.BookType);
        DomainHydrator.Set(book, nameof(JournalBook.IsActive), entity.IsActive);
        return book;
    }
}

internal sealed class AccountingReportRepository(ErpDbContext context) : IAccountingReportRepository
{
    public async Task<IReadOnlyList<TrialBalanceRow>> GetTrialBalanceAsync(
        Guid companyId,
        DateTime asOfDate,
        CancellationToken cancellationToken = default)
    {
        var postedStatus = (int)JournalEntryStatus.Posted;
        var endDate = UtcDateTimeNormalizer.ToUtc(asOfDate.Date.AddDays(1).AddTicks(-1));

        var lines = await (
            from line in context.JournalEntryLines.AsNoTracking()
            join entry in context.JournalEntries.AsNoTracking() on line.JournalEntryId equals entry.Id
            join account in context.Accounts.AsNoTracking() on line.AccountId equals account.Id
            where entry.CompanyId == companyId
                  && entry.Status == postedStatus
                  && entry.EntryDate <= endDate
                  && account.IsActive
            group new { line, account } by new
            {
                account.Id,
                account.Code,
                account.NameAr,
                account.AccountType
            }
            into g
            select new
            {
                g.Key.Id,
                g.Key.Code,
                g.Key.NameAr,
                g.Key.AccountType,
                Debit = g.Sum(x => x.line.Debit),
                Credit = g.Sum(x => x.line.Credit)
            }).ToListAsync(cancellationToken);

        return lines
            .Select(r =>
            {
                var accountType = AccountingDisplayExtensions.ParseAccountType(r.AccountType);
                var balance = accountType is GlAccountType.Asset or GlAccountType.Expense
                    ? r.Debit - r.Credit
                    : r.Credit - r.Debit;

                return new TrialBalanceRow
                {
                    AccountId = r.Id,
                    AccountCode = r.Code,
                    AccountName = r.NameAr,
                    AccountType = accountType,
                    DebitTotal = r.Debit,
                    CreditTotal = r.Credit,
                    Balance = balance
                };
            })
            .Where(r => r.DebitTotal != 0 || r.CreditTotal != 0)
            .OrderBy(r => r.AccountCode)
            .ToList();
    }

    public async Task<IReadOnlyList<AccountLedgerRow>> GetAccountLedgerAsync(
        Guid accountId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var postedStatus = (int)JournalEntryStatus.Posted;
        var start = UtcDateTimeNormalizer.ToUtc(fromDate.Date);
        var end = UtcDateTimeNormalizer.ToUtc(toDate.Date.AddDays(1).AddTicks(-1));

        var account = await context.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        if (account is null)
            return [];

        var accountType = AccountingDisplayExtensions.ParseAccountType(account.AccountType);
        var isDebitNormal = accountType is GlAccountType.Asset or GlAccountType.Expense;

        var raw = await (
            from line in context.JournalEntryLines.AsNoTracking()
            join entry in context.JournalEntries.AsNoTracking() on line.JournalEntryId equals entry.Id
            where line.AccountId == accountId
                  && entry.Status == postedStatus
                  && entry.EntryDate >= start
                  && entry.EntryDate <= end
            orderby entry.EntryDate, entry.EntryNumber, line.CreatedAt
            select new
            {
                entry.Id,
                entry.EntryNumber,
                entry.EntryDate,
                entry.Description,
                line.Narrative,
                line.Debit,
                line.Credit
            }).ToListAsync(cancellationToken);

        decimal running = 0m;
        var rows = new List<AccountLedgerRow>();
        foreach (var item in raw)
        {
            running += isDebitNormal
                ? item.Debit - item.Credit
                : item.Credit - item.Debit;

            rows.Add(new AccountLedgerRow
            {
                JournalEntryId = item.Id,
                EntryNumber = item.EntryNumber,
                EntryDate = item.EntryDate,
                Description = item.Description,
                LineNarrative = item.Narrative,
                Debit = item.Debit,
                Credit = item.Credit,
                RunningBalance = running
            });
        }

        return rows;
    }
}

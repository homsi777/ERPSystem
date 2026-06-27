using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;

namespace ERPSystem.Domain.Services;

public static class AccountingPostingPolicy
{
    public static void EnsureCanPost(AccountingAggregate entry)
    {
        if (entry.Status is JournalEntryStatus.Posted or JournalEntryStatus.Reversed or JournalEntryStatus.Cancelled)
            throw new AccountingException("Entry is already finalized.");

        if (entry.Lines.Count == 0)
            throw new AccountingException("Journal entry must have at least one line.");

        if (Math.Abs(entry.DebitTotal.Amount - entry.CreditTotal.Amount) > 0.01m)
            throw new AccountingException("Journal entry must be balanced before posting.");
    }

    public static bool IsImmutable(JournalEntryStatus status) =>
        status is JournalEntryStatus.Posted or JournalEntryStatus.Reversed;
}

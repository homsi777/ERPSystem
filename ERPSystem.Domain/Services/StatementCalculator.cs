using ERPSystem.Domain.Entities.Accounting;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Services;

public static class StatementCalculator
{
    public static IReadOnlyList<CustomerStatementEntry> BuildRunningBalance(
        IEnumerable<CustomerStatementEntry> entries)
    {
        var sorted = entries.OrderBy(e => e.EntryDate).ThenBy(e => e.Id).ToList();
        var running = Money.Zero();
        var result = new List<CustomerStatementEntry>();

        foreach (var entry in sorted)
        {
            running = running.Add(entry.Debit).Subtract(entry.Credit);
            result.Add(CustomerStatementEntry.Create(
                entry.CustomerId,
                entry.EntryDate,
                entry.DocumentType,
                entry.DocumentId,
                entry.Debit,
                entry.Credit,
                running));
        }

        return result;
    }

    public static Money CalculateSupplierBalance(IEnumerable<SupplierStatementEntry> entries) =>
        entries.Aggregate(Money.Zero(), (balance, entry) =>
            balance.Add(entry.Debit).Subtract(entry.Credit));
}

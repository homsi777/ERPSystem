using ERPSystem.Domain.Entities.Accounting;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Services;

public static class CustomerBalanceCalculator
{
    public static Money CalculateBalance(IEnumerable<CustomerStatementEntry> entries) =>
        entries.Aggregate(Money.Zero(), (balance, entry) =>
            balance.Add(entry.Debit).Subtract(entry.Credit));

    public static Money CalculateOutstanding(Money balance) =>
        balance.Amount >= 0 ? balance : Money.Zero();
}

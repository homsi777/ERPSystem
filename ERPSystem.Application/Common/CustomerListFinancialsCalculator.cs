using ERPSystem.Domain.Aggregates;

namespace ERPSystem.Application.Common;

/// <summary>
/// Single source for customer list financial columns — used by API (web) and WPF via the same DTO.
/// </summary>
public static class CustomerListFinancialsCalculator
{
    public static (decimal OpeningBalance, decimal ComputedBalance) Calculate(
        CustomerAggregate aggregate,
        decimal postedOpening,
        decimal pendingOpening,
        decimal invoiced,
        decimal receipts)
    {
        var opening = postedOpening + pendingOpening;

        if (opening <= 0 && aggregate.Customer.OpeningBalancePosted)
        {
            var implied = aggregate.Customer.Balance.Amount + receipts - invoiced;
            if (implied > 0)
                opening = implied;
        }

        var computed = opening + invoiced - receipts;
        return (opening, computed);
    }

    public static string NormalizePartyName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();

    public static bool NamesMatch(string? left, string? right) =>
        string.Equals(NormalizePartyName(left), NormalizePartyName(right), StringComparison.OrdinalIgnoreCase);
}

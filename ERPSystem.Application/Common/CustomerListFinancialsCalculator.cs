using ERPSystem.Application.DTOs.Customers;
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

    /// <summary>Dashboard + customer list header totals — same formula per customer row.</summary>
    public static (decimal TotalOutstanding, decimal TotalPostedReceipts) SumActiveCustomers(
        IEnumerable<CustomerAggregate> customers,
        IReadOnlyDictionary<Guid, CustomerListFinancialSummary> financials)
    {
        decimal outstanding = 0;
        decimal receipts = 0;

        foreach (var aggregate in customers.Where(c => c.Customer.IsActive))
        {
            financials.TryGetValue(aggregate.Customer.Id, out var fin);
            var postedOpening = fin?.PostedOpeningBalanceAmount ?? 0m;
            var pendingOpening = fin?.PendingOpeningBalanceAmount ?? 0m;
            var invoiced = fin?.TotalInvoiced ?? 0m;
            var customerReceipts = fin?.TotalReceipts ?? 0m;

            var (_, computed) = Calculate(
                aggregate,
                postedOpening,
                pendingOpening,
                invoiced,
                customerReceipts);

            outstanding += computed;
            receipts += customerReceipts;
        }

        return (outstanding, receipts);
    }
}

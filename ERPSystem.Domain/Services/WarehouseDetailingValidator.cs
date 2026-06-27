using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Sales;

namespace ERPSystem.Domain.Services;

public static class WarehouseDetailingValidator
{
    public static bool AllRollsHaveValidLength(IEnumerable<SalesInvoiceRollDetail> rollDetails) =>
        rollDetails.All(d => d.HasValidLength);

    public static bool CanCompleteDetailing(SalesInvoiceAggregate invoice) =>
        invoice.RollDetails.Count > 0 && AllRollsHaveValidLength(invoice.RollDetails);

    public static IReadOnlyList<string> GetMissingRollErrors(IEnumerable<SalesInvoiceRollDetail> rollDetails)
    {
        var errors = new List<string>();
        foreach (var detail in rollDetails.Where(d => !d.HasValidLength))
            errors.Add($"Roll {detail.RollSequence.Value} requires a valid length.");
        return errors;
    }
}

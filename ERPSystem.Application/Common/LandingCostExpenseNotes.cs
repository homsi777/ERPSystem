using ERPSystem.Domain.Entities.ChinaImport;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.Common;

public static class LandingCostExpenseNotes
{
    public static IReadOnlyList<LandingCostExpense> BuildLedger(
        decimal chinaInvoiceAmountUsd,
        string? chinaInvoiceNote,
        decimal shipping,
        string? shippingNote,
        decimal insurance,
        string? insuranceNote,
        decimal customsClearance,
        string? customsClearanceNote,
        decimal other1,
        string? other1Note,
        decimal other2,
        string? other2Note,
        decimal other3,
        string? other3Note,
        decimal other4,
        string? other4Note)
    {
        var ledger = new List<LandingCostExpense>();
        Add(ledger, LandingCostExpenseTypes.ChinaInvoice, chinaInvoiceAmountUsd, chinaInvoiceNote);
        Add(ledger, LandingCostExpenseTypes.Shipping, shipping, shippingNote);
        Add(ledger, LandingCostExpenseTypes.Insurance, insurance, insuranceNote);
        Add(ledger, LandingCostExpenseTypes.CustomsClearance, customsClearance, customsClearanceNote);
        Add(ledger, LandingCostExpenseTypes.OtherExpense1, other1, other1Note);
        Add(ledger, LandingCostExpenseTypes.OtherExpense2, other2, other2Note);
        Add(ledger, LandingCostExpenseTypes.OtherExpense3, other3, other3Note);
        Add(ledger, LandingCostExpenseTypes.OtherExpense4, other4, other4Note);
        return ledger;
    }

    public static string? GetNote(IEnumerable<LandingCostExpense> expenses, string expenseType) =>
        expenses.FirstOrDefault(e =>
            string.Equals(e.ExpenseType, expenseType, StringComparison.OrdinalIgnoreCase))?.Notes;

    private static void Add(List<LandingCostExpense> ledger, string expenseType, decimal amount, string? note)
    {
        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (amount == 0m && trimmed is null)
            return;

        ledger.Add(LandingCostExpense.Create(expenseType, new Money(amount), trimmed));
    }
}

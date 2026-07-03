using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;

namespace ERPSystem.Domain.Services;

public static class ExpenseLifecycle
{
  private static readonly IReadOnlyDictionary<ExpenseStatus, ExpenseStatus[]> AllowedTransitions =
        new Dictionary<ExpenseStatus, ExpenseStatus[]>
        {
            [ExpenseStatus.Draft] = [ExpenseStatus.PendingApproval, ExpenseStatus.Cancelled],
            [ExpenseStatus.PendingApproval] = [ExpenseStatus.Approved, ExpenseStatus.Cancelled],
            [ExpenseStatus.Approved] = [ExpenseStatus.Scheduled, ExpenseStatus.Cancelled],
            [ExpenseStatus.Scheduled] = [ExpenseStatus.PartiallyPaid, ExpenseStatus.Paid, ExpenseStatus.Cancelled],
            [ExpenseStatus.PartiallyPaid] = [ExpenseStatus.Paid, ExpenseStatus.Cancelled],
            [ExpenseStatus.Paid] = [ExpenseStatus.Closed],
            [ExpenseStatus.Closed] = [ExpenseStatus.Archived],
            [ExpenseStatus.Cancelled] = [ExpenseStatus.Archived],
            [ExpenseStatus.Archived] = []
        };

    public static IReadOnlyList<ExpenseStatus> GetAllowedTransitions(ExpenseStatus current) =>
        AllowedTransitions.TryGetValue(current, out var next) ? next : [];

    public static void ValidateTransition(ExpenseStatus from, ExpenseStatus to)
    {
        if (from == to)
            return;

        if (!GetAllowedTransitions(from).Contains(to))
            throw new ExpenseLifecycleException(
                $"Cannot transition expense from '{from}' to '{to}'. Allowed: {string.Join(", ", GetAllowedTransitions(from))}");
    }

    public static IReadOnlyList<(string Label, bool Completed, bool Current)> BuildStepper(ExpenseStatus status)
    {
        var labels = new[]
        {
            "مسودة",
            "بانتظار الاعتماد",
            "معتمد",
            "مجدول",
            "مدفوع جزئياً",
            "مدفوع",
            "مغلق",
            "ملغى",
            "مؤرشف"
        };

        var stage = StageIndex(status);
        return labels.Select((label, i) =>
        {
            var completed = i < stage;
            var current = i == stage;
            return (label, completed, current);
        }).ToList();
    }

    public static ExpenseStatus ResolvePaymentStatus(decimal baseAmount, decimal paidBase)
    {
        // مصروف مفتوح (بدون سقف ميزانية) — يبقى نشطاً دائماً
        if (baseAmount <= 0)
            return ExpenseStatus.Scheduled;

        if (paidBase <= 0)
            return ExpenseStatus.Scheduled;

        if (paidBase >= baseAmount)
            return ExpenseStatus.Paid;

        return ExpenseStatus.PartiallyPaid;
    }

    private static int StageIndex(ExpenseStatus status) => status switch
    {
        ExpenseStatus.Draft => 0,
        ExpenseStatus.PendingApproval => 1,
        ExpenseStatus.Approved => 2,
        ExpenseStatus.Scheduled => 3,
        ExpenseStatus.PartiallyPaid => 4,
        ExpenseStatus.Paid => 5,
        ExpenseStatus.Closed => 6,
        ExpenseStatus.Cancelled => 7,
        ExpenseStatus.Archived => 8,
        _ => 0
    };
}

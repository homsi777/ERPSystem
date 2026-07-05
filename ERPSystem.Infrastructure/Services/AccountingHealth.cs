using ERPSystem.Application.Common;
using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Services;

/// <summary>
/// One-time startup check that the minimum required GL accounts are configured.
/// The UI (Dashboard) reads <see cref="MissingRequiredAccounts"/> to show a
/// non-blocking warning banner when core accounts are absent.
/// </summary>
public static class AccountingHealth
{
    private static readonly (Guid Id, string Name)[] Required =
    [
        (AccountingAccountIds.AccountsReceivable, "الذمم المدينة"),
        (AccountingAccountIds.AccountsPayable, "الذمم الدائنة"),
        (AccountingAccountIds.InventoryAsset, "أصول المخزون"),
        (AccountingAccountIds.CostOfGoodsSold, "تكلفة المبيعات"),
        (AccountingAccountIds.OpeningBalanceEquity, "حقوق ملكية افتتاحية"),
        (AccountingAccountIds.CashUsd, "الصندوق")
    ];

    public static IReadOnlyList<string> MissingRequiredAccounts { get; private set; } = [];

    public static bool HasMissingAccounts => MissingRequiredAccounts.Count > 0;

    public static async Task ValidateAsync(ErpDbContext context, CancellationToken cancellationToken = default)
    {
        var requiredIds = Required.Select(r => r.Id).ToList();
        var present = await context.Accounts.AsNoTracking()
            .Where(a => requiredIds.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        MissingRequiredAccounts = Required
            .Where(r => !present.Contains(r.Id))
            .Select(r => r.Name)
            .ToList();
    }
}

using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Services;

namespace ERPSystem.Services.Expenses;

public static class ExpenseActionRouter
{
    public static bool TryHandle(EntityActionId actionId, EntityType entityType, object entityRow, AppModule sourceModule)
    {
        if (entityType != EntityType.Expense || entityRow is not ExpenseListDto row)
            return false;

        return ExpensePopupService.HandleAction(actionId, row);
    }

    public static bool TryHandleQuickAction(string? actionKey, Guid expenseId, string expenseName) =>
        ExpenseQuickActionRouter.TryHandleQuickAction(actionKey, expenseId, expenseName);
}

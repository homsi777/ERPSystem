using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Core.Actions;
using ERPSystem.Services;

namespace ERPSystem.Services.Expenses;

public static class ExpenseQuickActionRouter
{
    public static bool TryHandle(string actionKey, ExpenseDetailsDto expense)
    {
        var row = ToListDto(expense);
        return actionKey.ToLowerInvariant() switch
        {
            "nav:expenses:form" => ExpensePopupService.ShowEdit(row),
            "expense:record-payment" => ExpensePopupService.ShowEntry(row),
            "expense:schedule-payment" => ExpensePopupService.ShowOperationsCenter(row, "Installments"),
            "expense:duplicate" => ExpensePopupService.HandleAction(EntityActionId.ExpenseDuplicate, row),
            "expense:archive" => ExpensePopupService.HandleAction(EntityActionId.ExpenseArchive, row),
            "expense:approve" => ExpensePopupService.HandleAction(EntityActionId.ExpenseApprove, row),
            "expense:reject" => ExpensePopupService.HandleAction(EntityActionId.ExpenseReject, row),
            "expense:cancel" => ExpensePopupService.HandleAction(EntityActionId.ExpenseCancel, row),
            "expense:close" => HandleClose(row),
            _ => false
        };
    }

    public static bool TryHandleQuickAction(string? actionKey, Guid expenseId, string expenseName)
    {
        if (string.IsNullOrEmpty(actionKey))
            return false;

        return TryHandle(actionKey, new ExpenseDetailsDto
        {
            Id = expenseId,
            Name = expenseName,
            Code = ""
        });
    }

    private static bool HandleClose(ExpenseListDto row)
    {
        _ = CloseAsync(row);
        return true;
    }

    private static async Task CloseAsync(ExpenseListDto row)
    {
        var result = await ExpenseUiService.Instance.CloseAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
            ExpenseListRefreshHub.RequestRefresh();
    }

    private static ExpenseListDto ToListDto(ExpenseDetailsDto d) => new()
    {
        Id = d.Id,
        Code = d.Code,
        Name = d.Name,
        Status = d.Status,
        StatusDisplay = d.StatusDisplay,
        CategoryKindDisplay = d.CategoryName,
        PaidAmountBase = d.PaidAmountBase,
        BaseCurrency = d.BaseCurrency,
        IsArchived = d.Status == Domain.Enums.ExpenseStatus.Archived
    };
}

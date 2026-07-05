using ERPSystem.Application.Results;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Customers;
using ERPSystem.Core.Workspace;

namespace ERPSystem.Services.Customers;

public static class CustomerActionRouter
{
    public static bool TryHandle(EntityActionId actionId, EntityType entityType, object entityRow, AppModule sourceModule)
    {
        if (entityType != EntityType.Customer)
            return false;

        var row = entityRow as CustomerListRow;
        if (row is null)
            return false;

        switch (actionId)
        {
            case EntityActionId.CustomerEdit:
                CustomerNavigationContext.BeginEdit(row.Id);
                MockInteractionService.Navigate(AppModule.Customers, "Form");
                return true;

            case EntityActionId.CustomerDeactivate:
                _ = DeactivateAsync(row);
                return true;

            case EntityActionId.CustomerReceivables:
            case EntityActionId.CustomerStatement:
                MockInteractionService.OpenCustomerStatement(row);
                return true;

            case EntityActionId.CustomerPayment:
            case EntityActionId.CustomerReceipt:
                ERPSystem.Controls.Accounting.ReceiptVoucherNavigationContext.PreselectCustomerId = row.Id;
                MockInteractionService.Navigate(AppModule.Accounting, "Receipts");
                return true;

            default:
                return false;
        }
    }

    public static bool TryHandleQuickAction(string? actionKey, OperationsCenterContext ctx)
    {
        if (ctx.EntityRow is not CustomerListRow row)
            return false;

        switch (actionKey)
        {
            case "form:EditCustomer":
                CustomerNavigationContext.BeginEdit(row.Id);
                MockInteractionService.Navigate(AppModule.Customers, "Form");
                return true;

            case "ws:DeactivateCustomer":
                _ = DeactivateAsync(row);
                return true;

            default:
                return false;
        }
    }

    private static async Task DeactivateAsync(CustomerListRow row)
    {
        if (!await CustomerUiService.Instance.CanDeactivateAsync())
        {
            ApplicationResultPresenter.Present(
                ApplicationResult.PermissionDenied("Not allowed to deactivate customers."));
            return;
        }

        var result = await CustomerUiService.Instance.DeactivateAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
            CustomerListRefreshHub.RequestRefresh();
    }
}

public static class CustomerListRefreshHub
{
    public static event EventHandler? RefreshRequested;

    public static void RequestRefresh() => RefreshRequested?.Invoke(null, EventArgs.Empty);
}

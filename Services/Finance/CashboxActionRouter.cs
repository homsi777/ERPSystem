using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Helpers;
using ERPSystem.Services;

namespace ERPSystem.Services.Finance;

public static class CashboxActionRouter
{
    public static bool TryHandle(EntityActionId actionId, EntityType entityType, object entityRow, AppModule sourceModule)
    {
        if (entityType != EntityType.Cashbox || entityRow is not CashboxListDto row)
            return false;

        Handle(actionId, row, sourceModule);
        return true;
    }

    public static void Handle(EntityActionId actionId, CashboxListDto row, AppModule sourceModule)
    {
        switch (actionId)
        {
            case EntityActionId.OpenOperationsCenter:
                CashboxPopupService.ShowOperationsCenter(row.Id);
                break;
            case EntityActionId.CashboxEdit:
                CashboxPopupService.ShowEdit(row.Id);
                break;
            case EntityActionId.CashboxTransfer:
                CashboxPopupService.ShowTransfer(row.Id);
                break;
            case EntityActionId.CashboxMovements:
                CashboxPopupService.ShowOperationsCenter(row.Id, "Movements");
                break;
            case EntityActionId.CashboxProperties:
                _ = ShowPropertiesAsync(row);
                break;
            case EntityActionId.CustomerReceipt:
                MockInteractionService.Navigate(AppModule.Accounting, "Receipts");
                break;
            case EntityActionId.CustomerPayment:
                MockInteractionService.Navigate(AppModule.Accounting, "Payments");
                break;
            case EntityActionId.CashboxDeactivate:
                _ = DeactivateAsync(row);
                break;
            case EntityActionId.CashboxActivate:
                _ = ActivateAsync(row);
                break;
        }
    }

    private static async Task ShowPropertiesAsync(CashboxListDto row)
    {
        if (!AppServices.IsInitialized) return;
        var result = await FinanceUiService.Instance.GetCashboxDetailsAsync(row.Id);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;

        var d = result.Value;
        var sp = new System.Windows.Controls.StackPanel();
        sp.Children.Add(Helpers.ErpUiFactory.BuildFormGrid(
            ("الكود", T(d.Code)),
            ("الاسم", T(d.Name)),
            ("الرصيد", T($"{d.Balance:N2} {d.Currency}")),
            ("العملة", T(d.Currency)),
            ("الحالة", T(d.IsActive ? "نشط" : "معطل")),
            ("قبض اليوم", T($"{d.TodayReceipts:N2}")),
            ("صرف اليوم", T($"{d.TodayPayments:N2}"))));

        Dialogs.ErpModalWindow.Show("خصائص الصندوق", d.Name, sp, "\uE946", 480, 420);
    }

    private static System.Windows.Controls.TextBlock T(string text) =>
        new() { Text = text, TextWrapping = System.Windows.TextWrapping.Wrap };

    private static async Task DeactivateAsync(CashboxListDto row)
    {
        if (!AppServices.IsInitialized) return;
        var result = await FinanceUiService.Instance.DeactivateCashboxAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
            CashboxListRefreshHub.RequestRefresh();
    }

    private static async Task ActivateAsync(CashboxListDto row)
    {
        if (!AppServices.IsInitialized) return;
        var result = await FinanceUiService.Instance.ActivateCashboxAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
            CashboxListRefreshHub.RequestRefresh();
    }
}

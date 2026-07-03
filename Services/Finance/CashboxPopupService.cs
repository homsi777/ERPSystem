using ERPSystem.Controls.Finance;
using ERPSystem.Dialogs;
using ERPSystem.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Services.Finance;

public static class CashboxPopupService
{
    private static ErpModalWindow? _active;

    public static bool ShowCreate()
    {
        CashboxNavigationContext.BeginCreate();
        return ShowForm("صندوق جديد", "إضافة صندوق نقدي", "\uE825", () => new CashboxFormPopupControl());
    }

    public static bool ShowEdit(Guid cashboxId)
    {
        CashboxNavigationContext.BeginEdit(cashboxId);
        return ShowForm("تعديل صندوق", "تحديث بيانات الصندوق", "\uE70F", () => new CashboxFormPopupControl());
    }

    public static bool ShowTransfer(Guid? fromCashboxId = null)
    {
        CashboxNavigationContext.BeginTransfer(fromCashboxId);
        _active = new ErpModalWindow();
        _active.Configure("تحويل بين الصناديق", "خصم من المصدر وإضافة للوجهة فوراً", "\uE8AB", 520, 560);
        _active.SetBody(new CashboxTransferFormPopupControl());
        var result = _active.ShowDialog();
        _active = null;
        CashboxNavigationContext.TransferFromCashboxId = null;
        return result == true;
    }

    public static void ShowOperationsCenter(Guid cashboxId, string? initialTab = null)
    {
        var oc = new CashboxOperationsCenterControl();
        oc.InitializeForPopup(cashboxId, initialTab);
        ErpModalWindow.Show(
            "مركز عمل الصندوق",
            "حركات وتحويلات — بيانات حية",
            oc,
            "\uE825",
            1000,
            720);
    }

    public static void CloseActive(bool success = true)
    {
        CashboxListRefreshHub.RequestRefresh();
        if (_active is null) return;
        _active.DialogResult = success;
        _active.Close();
    }

    public static void CancelActive()
    {
        if (_active is null) return;
        _active.DialogResult = false;
        _active.Close();
    }

    private static bool ShowForm(string title, string subtitle, string icon, Func<UserControl> factory)
    {
        _active = new ErpModalWindow();
        _active.Configure(title, subtitle, icon, 520, 480);
        _active.SetBody(factory());
        var result = _active.ShowDialog();
        _active = null;
        CashboxNavigationContext.EditCashboxId = null;
        return result == true;
    }
}

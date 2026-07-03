using ERPSystem.Controls.Finance;
using ERPSystem.Dialogs;
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
        return ShowDialog(
            new CashboxTransferFormPopupControl(),
            "تحويل بين الصناديق",
            "خصم من المصدر وإضافة للوجهة فوراً",
            "\uE8AB",
            520,
            560) == true;
    }

    public static void ShowOperationsCenter(Guid cashboxId, string? initialTab = null)
    {
        var oc = new CashboxOperationsCenterControl();
        oc.InitializeForPopup(cashboxId, initialTab);
        ShowDialog(
            oc,
            "مركز عمل الصندوق",
            "حركات وتحويلات — بيانات حية",
            "\uE825",
            1000,
            720);
        CashboxListRefreshHub.RequestRefresh();
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
        var result = ShowDialog(factory(), title, subtitle, icon, 520, 480);
        CashboxNavigationContext.EditCashboxId = null;
        return result == true;
    }

    private static bool? ShowDialog(
        UIElement content,
        string title,
        string subtitle,
        string icon,
        double width,
        double maxHeight = 680)
    {
        _active = new ErpModalWindow();
        _active.Configure(title, subtitle, icon, width, maxHeight);
        _active.SetBody(content);
        var result = _active.ShowDialog();
        _active = null;
        CashboxNavigationContext.TransferFromCashboxId = null;
        return result;
    }
}

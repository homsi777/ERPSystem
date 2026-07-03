using ERPSystem.Controls.Inventory;
using ERPSystem.Dialogs;
using ERPSystem.Services;
using System.Windows;

namespace ERPSystem.Services.Inventory;

public static class InventoryPopupService
{
    private static ErpModalWindow? _active;

    public static bool ShowCreateWarehouse() =>
        ShowWarehouseForm(
            "مستودع جديد",
            "إضافة مستودع للأقمشة والمخزون",
            "\uE8B7",
            () =>
            {
                InventoryNavigationContext.BeginCreate();
                var form = new InventoryWarehouseFormControl();
                form.BindPopupHost();
                return form;
            });

    public static bool ShowEditWarehouse(Guid warehouseId) =>
        ShowWarehouseForm(
            "تعديل مستودع",
            "تحديث بيانات المستودع",
            "\uE70F",
            () =>
            {
                InventoryNavigationContext.BeginEdit(warehouseId);
                var form = new InventoryWarehouseFormControl();
                form.BindPopupHost();
                return form;
            });

    public static bool ShowTransferWizard(Guid? fromWarehouseId = null, Guid? transferId = null)
    {
        if (transferId.HasValue)
            InventoryNavigationContext.BeginEditTransfer(transferId.Value);
        else
            InventoryNavigationContext.BeginCreateTransfer(fromWarehouseId);

        _active = new ErpModalWindow();
        _active.Configure("مناقلة مخزنية", "معالج مناقلة من 5 خطوات — مخزون حقيقي فقط", "\uE7BF", 820, 720);
        _active.SetBody(new InventoryTransferWizardControl());
        var result = _active.ShowDialog();
        _active = null;
        InventoryNavigationContext.EditTransferId = null;
        return result == true;
    }

    public static bool ShowStocktakeWizard(Guid? warehouseId = null, Guid? sessionId = null)
    {
        if (sessionId.HasValue)
            InventoryNavigationContext.BeginEditStocktake(sessionId.Value);
        else
            InventoryNavigationContext.BeginCreateStocktake(warehouseId);

        _active = new ErpModalWindow();
        _active.Configure("جرد مخزني", "مسودة → عد → مراجعة → ترحيل", "\uE787", 900, 760);
        _active.SetBody(new InventoryStocktakeWizardControl());
        var result = _active.ShowDialog();
        _active = null;
        InventoryNavigationContext.EditStocktakeId = null;
        return result == true;
    }

    public static void CompleteSuccess()
    {
        InventoryListRefreshHub.RequestRefresh();
        if (_active is null) return;
        _active.DialogResult = true;
        _active.Close();
    }

    public static void CancelActive()
    {
        if (_active is null) return;
        _active.DialogResult = false;
        _active.Close();
    }

    private static bool ShowWarehouseForm(
        string title,
        string subtitle,
        string icon,
        Func<InventoryWarehouseFormControl> factory)
    {
        var form = factory();
        _active = new ErpModalWindow();
        _active.Configure(title, subtitle, icon, 560, 720);
        _active.SetBody(form);
        var result = _active.ShowDialog();
        _active = null;
        InventoryNavigationContext.EditWarehouseId = null;
        return result == true;
    }
}

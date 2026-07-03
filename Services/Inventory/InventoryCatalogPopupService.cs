using ERPSystem.Application.DTOs.Catalog;
using ERPSystem.Controls.Inventory;
using ERPSystem.Dialogs;

namespace ERPSystem.Services.Inventory;

public static class InventoryCatalogPopupService
{
    private static ErpModalWindow? _active;

    public static bool ShowEditClassification(ImportedFabricClassificationDto row) =>
        ShowForm("تعديل تصنيف", row.DisplayLabel, "\uE70F", 560, () =>
        {
            InventoryCatalogNavigationContext.BeginEditClassification(row);
            var form = new ImportedFabricClassificationFormControl();
            form.BindPopupHost();
            return form;
        });

    public static void CompleteSuccess()
    {
        InventoryCatalogListRefreshHub.RequestRefresh();
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

    private static bool ShowForm(
        string title, string subtitle, string icon, double width, Func<System.Windows.Controls.UserControl> factory)
    {
        _active = new ErpModalWindow();
        _active.Configure(title, subtitle, icon, width, 720);
        _active.SetBody(factory());
        var result = _active.ShowDialog();
        _active = null;
        InventoryCatalogNavigationContext.EditClassification = null;
        return result == true;
    }
}

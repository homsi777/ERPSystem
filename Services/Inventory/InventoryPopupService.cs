using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Controls.Inventory;
using ERPSystem.Dialogs;
using ERPSystem.Helpers;
using ERPSystem.Services;
using System.Windows;
using System.Windows.Controls;

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

    public static void ShowMovementDetail(WarehouseMovementCardDto movement)
    {
        var sp = new StackPanel();
        sp.Children.Add(ErpUiFactory.SectionTitle("تفاصيل الحركة"));
        sp.Children.Add(ErpUiFactory.BuildFormGrid(
            ("الرقم", new TextBlock { Text = movement.MovementNumber }),
            ("النوع", new TextBlock { Text = movement.Type }),
            ("من", new TextBlock { Text = movement.FromLabel }),
            ("إلى", new TextBlock { Text = movement.ToLabel }),
            ("الكمية", new TextBlock { Text = $"{movement.QuantityMeters:N2} م" }),
            ("القيمة", new TextBlock { Text = $"${movement.TotalValue:N2}" }),
            ("المستخدم", new TextBlock { Text = movement.Username }),
            ("التاريخ", new TextBlock { Text = movement.Timestamp.ToString("yyyy/MM/dd HH:mm") }),
            ("المرجع", new TextBlock { Text = movement.ReferenceType ?? "—" })));

        ErpModalWindow.Show("حركة مخزنية", movement.MovementNumber, sp, "\uE8CB", 480, 520);
    }

    public static void ShowWarehouseWorkspace(Guid warehouseId, string? initialTab = null)
    {
        var oc = new InventoryOperationsCenterControl();
        oc.InitializeForPopup(warehouseId, initialTab);
        ErpModalWindow.Show(
            "مركز عمل المستودع",
            "لوحة تحكم تنفيذية — بيانات حية",
            oc,
            "\uE8B7",
            1180,
            860);
    }

    public static void ShowWarehousePanel(Guid warehouseId, WarehousePopupPanel panel)
    {
        var (title, icon) = panel switch
        {
            WarehousePopupPanel.Stock => ("تقرير المخزون", "\uE9D2"),
            WarehousePopupPanel.Movements => ("حركات المستودع", "\uE8CB"),
            WarehousePopupPanel.Timeline => ("الخط الزمني", "\uE823"),
            WarehousePopupPanel.Audit => ("سجل التدقيق", "\uE7C3"),
            _ => ("المستودع", "\uE8B7")
        };
        ErpModalWindow.Show(title, "PostgreSQL — live", new WarehousePanelPopupControl(warehouseId, panel), icon, 720, 640);
    }

    public static async void ShowWarehouseProperties(Guid warehouseId)
    {
        if (!AppServices.IsInitialized) return;
        var result = await InventoryUiService.Instance.GetWarehouseDetailAsync(warehouseId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;

        var d = result.Value;
        var sp = new StackPanel();
        sp.Children.Add(ErpUiFactory.BuildFormGrid(
            ("الكود", T(d.Code)),
            ("الاسم", T(d.NameAr)),
            ("الاسم EN", T(d.NameEn ?? "—")),
            ("المدينة", T(d.City)),
            ("العنوان", T(d.Address ?? "—")),
            ("المدير", T(d.Manager ?? "—")),
            ("Rolls", T(d.RollCount.ToString())),
            ("الأمتار", T($"{d.TotalMeters:N1}")),
            ("قيمة المخزون", T($"${d.InventoryValue:N2}")),
            ("افتراضي", T(d.IsDefault ? "نعم" : "لا")),
            ("نشط", T(d.IsActive ? "نعم" : "لا")),
            ("مؤرشف", T(d.IsArchived ? "نعم" : "لا")),
            ("آخر حركة", T(d.LastMovement ?? "—")),
            ("آخر جرد", T(d.LastStocktake ?? "—")),
            ("تاريخ الإنشاء", T(d.CreatedAt.ToString("yyyy/MM/dd")))));

        ErpModalWindow.Show("خصائص المستودع", d.NameAr, sp, "\uE946", 520, 680);
    }

    public static void ShowWarehousePrintPreview(WarehouseListExtendedDto warehouse) =>
        _ = WarehouseDocumentService.ShowStockPreviewAsync(warehouse.Id, exportPdf: false);

    private static TextBlock T(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };

    public static bool ShowOpeningStockForm(Guid? warehouseId = null)
    {
        InventoryNavigationContext.BeginCreateOpeningStock(warehouseId);
        _active = new ErpModalWindow();
        _active.Configure("مواد أول المدة", "إدخال أرصدة افتتاحية", "\uE8C8", 820, 720);
        _active.SetBody(new InventoryOpeningStockFormControl());
        var result = _active.ShowDialog();
        _active = null;
        InventoryNavigationContext.EditOpeningStockId = null;
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

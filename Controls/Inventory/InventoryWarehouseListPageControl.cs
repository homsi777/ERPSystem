using ERPSystem.Application.Commands.Inventory;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryWarehouseListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private bool _isLoading;

    public InventoryWarehouseListPageControl()
    {
        _page.Configure(EntityType.Warehouse, AppModule.Inventory);
        _page.SetHeader("المستودعات", "إدارة مستودعات الأقمشة والمخزون", "\uE8B7", B("AccentInventoryBrush"));
        _page.SetPrimaryButton("إضافة مستودع");
        _page.SetEmptyState("لا توجد مستودعات مضافة بعد", "إضافة مستودع", "\uE8B7");
        _page.PrimaryActionRequested += (_, _) => InventoryPopupService.ShowCreateWarehouse();

        _page.Grid.MouseDoubleClick += (_, _) =>
        {
            if (_page.Grid.SelectedItem is WarehouseListExtendedDto w)
            {
                InventoryNavigationContext.BeginWorkspace(w.Id);
                NavigationStateManager.Instance.NavigateTo(AppModule.Inventory, "WarehouseOperationsCenter");
            }
        };

        var g = _page.Grid;
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        foreach (var (h, p, w) in new (string, string, object)[]
        {
            ("الكود", nameof(WarehouseListExtendedDto.Code), 90),
            ("الاسم", nameof(WarehouseListExtendedDto.NameAr), "*"),
            ("المدينة", nameof(WarehouseListExtendedDto.City), 100),
            ("المدير", nameof(WarehouseListExtendedDto.Manager), 120),
            ("Rolls", nameof(WarehouseListExtendedDto.RollCount), 80),
            ("الأمتار", nameof(WarehouseListExtendedDto.TotalMeters), 100),
            ("القيمة $", nameof(WarehouseListExtendedDto.InventoryValue), 110),
            ("افتراضي", nameof(WarehouseListExtendedDto.IsDefault), 70),
            ("الحالة", nameof(WarehouseListExtendedDto.IsActive), 80)
        })
            ErpUiFactory.AddGridColumn(g, h, p, w, null);

        Content = _page;
        Loaded += OnLoaded;
        InventoryListRefreshHub.RefreshRequested += (_, _) => _ = LoadAsync();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isLoading || !AppServices.IsInitialized) { _page.BindData([]); return; }
        _isLoading = true;
        _page.SetLoadingState(true);
        try
        {
            var result = await InventoryUiService.Instance.GetWarehousesAsync();
            if (!result.IsSuccess || result.Value is null)
            {
                _page.BindData([]);
                return;
            }

            _page.BindData(result.Value.Cast<object>().ToList());
        }
        finally
        {
            _page.SetLoadingState(false);
            _isLoading = false;
        }
    }

    private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
}

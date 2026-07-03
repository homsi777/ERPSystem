using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Domain;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Sales;
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
        _page.SetHeader("المستودعات", "", "\uE8B7", B("AccentInventoryBrush"));
        _page.SetPrimaryButton("إضافة مستودع");
        _page.SetEmptyState("لا توجد مستودعات مضافة بعد", "إضافة مستودع", "\uE8B7");
        _page.PrimaryActionRequested += (_, _) =>
            MockInteractionService.ShowComingSoon("إضافة مستودع");

        var g = _page.Grid;
        g.AutoGenerateColumns = false;
        foreach (var (h, p, w) in new (string, string, object)[]
        {
            ("الكود", nameof(WarehouseEntity.Code), 90),
            ("الاسم", nameof(WarehouseEntity.Name), "*"),
            ("المدينة", nameof(WarehouseEntity.City), 100),
            ("الحالة", nameof(WarehouseEntity.Status), 80)
        })
        {
            ErpUiFactory.AddGridColumn(g, h, p, w, null);
        }

        Content = _page;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isLoading || !AppServices.IsInitialized)
        {
            _page.BindData([]);
            return;
        }

        _isLoading = true;
        _page.SetLoadingState(true);
        try
        {
            var result = await SalesUiService.Instance.GetWarehousesAsync();
            if (!result.IsSuccess || result.Value is null || result.Value.Count == 0)
            {
                _page.BindData([]);
                return;
            }

            var rows = result.Value
                .Select(w => new WarehouseEntity
                {
                    Code = w.Code,
                    Name = w.NameAr,
                    City = w.City ?? "—",
                    Status = w.IsActive ? "نشط" : "معطل"
                })
                .Cast<object>()
                .ToList();

            _page.BindData(rows);
        }
        finally
        {
            _page.SetLoadingState(false);
            _isLoading = false;
        }
    }

    private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
}

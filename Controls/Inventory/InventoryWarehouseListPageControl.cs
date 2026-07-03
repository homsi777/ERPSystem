using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryWarehouseListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private DataGridTemplateColumn? _tasksColumn;
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
                InventoryPopupService.ShowWarehouseWorkspace(w.Id);
        };

        // ⋮ — بديل لزر اليمين (نفس ExpenseContextMenuService.Show)
        _page.Grid.PreviewMouseLeftButtonUp += OnGridTasksClick;

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

        AddActionsColumn(g);

        Content = _page;
        Loaded += OnLoaded;
        InventoryListRefreshHub.RefreshRequested += (_, _) => _ = LoadAsync();
    }

    private void OnGridTasksClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<DataGridCell>(e.OriginalSource as DependencyObject) is not { Column: var col, DataContext: WarehouseListExtendedDto w } cell)
            return;
        if (_tasksColumn is null || col != _tasksColumn) return;

        e.Handled = true;
        WarehouseContextMenuService.Show(w, cell);
    }

    private void AddActionsColumn(DataGrid grid)
    {
        _tasksColumn = new DataGridTemplateColumn
        {
            Header = "مهام",
            Width = 72,
            IsReadOnly = true
        };

        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(Border));
        factory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        factory.SetValue(Border.PaddingProperty, new Thickness(4));
        factory.SetValue(Border.CursorProperty, Cursors.Hand);
        factory.SetValue(Border.ToolTipProperty, "قائمة المهام");

        var icon = new FrameworkElementFactory(typeof(TextBlock));
        icon.SetValue(TextBlock.TextProperty, "\uE712");
        icon.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Segoe MDL2 Assets"));
        icon.SetValue(TextBlock.FontSizeProperty, 16.0);
        icon.SetValue(TextBlock.ForegroundProperty, B("PrimaryBrush"));
        icon.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        icon.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        factory.AppendChild(icon);

        template.VisualTree = factory;
        _tasksColumn.CellTemplate = template;
        grid.Columns.Add(_tasksColumn);
    }

    private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T match) return match;
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
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

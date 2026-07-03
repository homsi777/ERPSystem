using ERPSystem.Controls;
using ERPSystem.Controls.Inventory;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services.Inventory;
using ERPSystem.Views.Reports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Views.Inventory;

/// <summary>يفتح معالج المناقلة عند التنقل المباشر لهذا المسار.</summary>
public sealed class InventoryTransferFormLauncherControl : UserControl
{
    public InventoryTransferFormLauncherControl() => Loaded += OnLoaded;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        var id = InventoryNavigationContext.EditTransferId;
        var from = InventoryNavigationContext.TakePreselectedFromWarehouse();
        InventoryPopupService.ShowTransferWizard(from, id);
        NavigationStateManager.Instance.NavigateTo(AppModule.Inventory, "Transfers");
    }
}

/// <summary>يفتح معالج الجرد عند التنقل المباشر لهذا المسار.</summary>
public sealed class InventoryStocktakeFormLauncherControl : UserControl
{
    public InventoryStocktakeFormLauncherControl() => Loaded += OnLoaded;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        var wh = InventoryNavigationContext.TakePreselectedStocktakeWarehouse();
        var id = InventoryNavigationContext.EditStocktakeId;
        InventoryPopupService.ShowStocktakeWizard(wh, id);
        NavigationStateManager.Instance.NavigateTo(AppModule.Inventory, "Stocktake");
    }
}

public static class InventoryViews
{
    private static readonly HashSet<string> KnownRouteKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Dashboard", "Categories", "ImportExcel", "OpeningStock", "Stocktake", "Transfers",
        "Settings", "Reports", "Warehouses", "WarehouseForm", "WarehouseOperationsCenter",
        "TransferForm", "StocktakeForm", "OpeningStockForm"
    };

    public static bool IsKnownRoute(string key) => KnownRouteKeys.Contains(key);

    public static UserControl Create(string key) => key switch
    {
        "Dashboard" => Wrap(new InventoryDashboardControl()),
        "Categories" => Wrap(new InventoryFabricCategoriesPageControl()),
        "ImportExcel" => BuildImportExcel(),
        "OpeningStock" => Wrap(new InventoryOpeningStockPageControl()),
        "Stocktake" => Wrap(new InventoryStocktakeListPageControl()),
        "Transfers" => Wrap(new InventoryTransferListPageControl()),
        "Settings" => BuildSettings(),
        "Reports" => ModuleReportsViews.CreateHub(AppModule.Inventory),
        "Warehouses" => BuildWarehousesHub(),
        "WarehouseForm" => Wrap(new InventoryWarehouseFormControl()),
        "WarehouseOperationsCenter" => Wrap(new InventoryOperationsCenterControl()),
        "TransferForm" => Wrap(new InventoryTransferFormLauncherControl()),
        "StocktakeForm" => Wrap(new InventoryStocktakeFormLauncherControl()),
        "OpeningStockForm" => Wrap(new InventoryOpeningStockFormControl()),
        _ => BuildWarehousesHub()
    };

    private static UserControl BuildWarehousesHub()
    {
        var tabs = new TabControl { FontFamily = new FontFamily("Segoe UI, Tahoma, Arial") };
        tabs.Items.Add(new TabItem { Header = "مراكز المستودعات", Content = new InventoryWarehouseListPageControl() });
        tabs.Items.Add(new TabItem { Header = "أرصدة الأقمشة", Content = new InventoryFabricStockPageControl() });
        return Wrap(new ScrollViewer { Content = tabs, Padding = new Thickness(16), VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }

    private static UserControl BuildImportExcel()
    {
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();
        stack.Children.Add(ErpUiFactory.SectionTitle("استيراد Excel للمخزون"));
        stack.Children.Add(ErpUxFactory.InfoBanner("استيراد أرصدة الافتتاح عبر معالج مواد أول المدة أو China Import."));
        stack.Children.Add(PlaceholderUi.EmptyMessage(
            "استيراد Excel",
            "استخدم China Import لاستيراد الحاويات، أو مواد أول المدة للأرصدة الافتتاحية"));
        root.Content = stack;
        return Wrap(root);
    }

    private static UserControl BuildSettings()
    {
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();
        stack.Children.Add(ErpUiFactory.SectionTitle("إعدادات المخزون"));
        stack.Children.Add(ErpUiFactory.Card(new StackPanel
        {
            Children =
            {
                new TextBlock { Text = "قواعد المخزون", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) },
                new TextBlock { Text = "• حد أدنى / أقصى / نقطة إعادة الطلب\n• استرategie تخصيص الدفعات (FIFO/LIFO)\n• التنبيهات التلقائية", TextWrapping = TextWrapping.Wrap }
            }
        }));
        root.Content = stack;
        return Wrap(root);
    }

    private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
    private static UserControl Wrap(UIElement c) => new() { Content = c };
}

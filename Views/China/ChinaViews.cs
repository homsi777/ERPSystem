using ERPSystem.Controls.China;
using ERPSystem.Core;
using ERPSystem.Views.Reports;
using ERPSystem.Helpers;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Views.China;

public static class ChinaViews
{
    private static readonly HashSet<string> KnownRouteKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Containers",
        "NewImport",
        "FileAnalysis",
        "CostEntry",
        "LandingCost",
        "SalePrice",
        "MoveToWarehouse",
        "ReadyForSale",
        "Distribution",
        "Stocktake",
        "Reports"
    };

    public static bool IsKnownRoute(string key) => KnownRouteKeys.Contains(key);

    public static UserControl Create(string key) => key switch
    {
        "NewImport" => new NewChinaImportControl(),
        "FileAnalysis" => new PackingListAnalysisControl(),
        "CostEntry" => new ChinaImportCostEntryControl(),
        "LandingCost" => new ChinaImportLandingCostReviewControl(),
        "SalePrice" => new ChinaImportSalePriceControl(),
        "MoveToWarehouse" => new ChinaImportWarehouseTransferControl(),
        "ReadyForSale" => new ChinaImportReadyForSaleControl(),
        "Distribution" => Wrap(new ContainerWorkflowSummaryControl(
            "توزيع الكميات على العملاء",
            "توزيع أثواب الحاوية على المشترين والحجوزات")),
        "Stocktake" => Wrap(new ContainerWorkflowSummaryControl(
            "جرد الحاوية",
            "مقارنة النظام مع العد الفعلي داخل الحاوية",
            stocktakeMode: true)),
        "Reports" => ModuleReportsViews.CreateHub(AppModule.ChinaImport),
        _ => new ContainerListPageControl()
    };

    private static Brush Br(string key) => (Brush)System.Windows.Application.Current.Resources[key]!;

    private static UserControl Wrap(UIElement content)
    {
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(16),
            Content = content
        };
        return new UserControl { Content = scroll, Background = Br("AppBgBrush") as SolidColorBrush };
    }
}

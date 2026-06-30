using ERPSystem.Controls.China;
using ERPSystem.Controls;
using ERPSystem.Core.Domain;
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
        "MoveToWarehouse",
        "ReadyForSale",
        "Distribution",
        "Stocktake"
    };

    public static bool IsKnownRoute(string key) => KnownRouteKeys.Contains(key);

    public static UserControl Create(string key) => key switch
    {
        "NewImport" => new NewChinaImportControl(),
        "FileAnalysis" => new PackingListAnalysisControl(),
        "CostEntry" => new ChinaImportCostEntryControl(),
        "LandingCost" => new ChinaImportLandingCostReviewControl(),
        "MoveToWarehouse" => new ChinaImportWarehouseTransferControl(),
        "ReadyForSale" => new ChinaImportReadyForSaleControl(),
        "Distribution" => BuildDistribution(),
        "Stocktake" => BuildStocktake(),
        _ => new ContainerListPageControl()
    };

    private static UserControl BuildDistribution()
    {
        var data = new[]
        {
            new ContainerCustomerDistribution { CustomerName = "أحمد الحمصي", FabricCode = "FAB-101", Color = "أبيض", Rolls = 12, Meters = 720 },
            new ContainerCustomerDistribution { CustomerName = "مؤسسة النسيج", FabricCode = "FAB-102", Color = "بيج", Rolls = 8, Meters = 480 },
        };
        return SimpleTablePage("توزيع الكميات على العملاء", "توزيع أثواب الحاوية على المشترين والحجوزات", data);
    }

    private static UserControl BuildStocktake()
    {
        var data = new[]
        {
            new { البند = "الوارد", القيمة = "450" }, new { البند = "المتوقع", القيمة = "448" },
            new { البند = "المعدود", القيمة = "446" }, new { البند = "الفرق", القيمة = "-2" },
            new { البند = "مبيعات", القيمة = "12" }, new { البند = "حجوزات", القيمة = "35" },
            new { البند = "إرجاع", القيمة = "1" }, new { البند = "هالك", القيمة = "3" },
            new { البند = "مناقلات", القيمة = "5" },
        };
        return SimpleTablePage("جرد الحاوية", "مقارنة النظام مع العد الفعلي داخل الحاوية", data);
    }

    private static UserControl SimpleTablePage(string title, string subtitle, IEnumerable data)
    {
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();
        stack.Children.Add(ErpUiFactory.SectionTitle(title));
        stack.Children.Add(new TextBlock { Text = subtitle, Foreground = Br("TextSecondaryBrush"), Margin = new Thickness(0, 0, 0, 16) });
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildGrid(data)));
        root.Content = stack;
        return Wrap(root);
    }

    private static Brush Br(string key) => (Brush)System.Windows.Application.Current.Resources[key]!;

    private static UserControl Wrap(UIElement content)
    {
        return new UserControl { Content = content, Background = Br("AppBgBrush") as SolidColorBrush };
    }
}

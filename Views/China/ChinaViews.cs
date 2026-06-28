using ERPSystem.Controls.China;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.ChinaImport;
using ERPSystem.Core.Domain;
using ERPSystem.Core.Workspace;
using ERPSystem.Helpers;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ERPSystem.Views.China
{
    public static class ChinaViews
    {
        public static UserControl Create(string key) => key switch
        {
            "NewImport" => new NewChinaImportControl(),
            "FileAnalysis" => new PackingListAnalysisControl(),
            "ExcelReview" => new PackingListAnalysisControl(),
            "Distribution" => BuildDistribution(),
            "Stocktake" => BuildStocktake(),
            "LandingCost" => BuildLandingCost(),
            _ => new ContainerListPageControl()
        };

        private static UserControl BuildLandingCost()
        {
            var cost = new ContainerLandingCost
            {
                TotalLengthFromInvoice = 38500,
                ContainerWeightKg = 18500,
                CustomsAmountPaid = 42000,
                Shipping = 15000,
                Clearance = 8500,
                OtherExpenses = 3200
            };

            var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUxFactory.WorkflowStepper(
                ("وصول", true, true), ("Excel", true, true), ("مراجعة", true, true),
                ("Landing Cost", true, false), ("اعتماد", false, false), ("مخزن", false, false), ("بيع", false, false)));
            stack.Children.Add(ErpUiFactory.SectionTitle("ملخص تكلفة الاستيراد — مراجعة مالية"));
            stack.Children.Add(ErpUxFactory.InfoBanner("راجع تكلفة المتر قبل اعتماد الحاوية وتحويلها للمخزن. البيع بالمتر — الوزن للتحقق فقط.", "info"));

            var form = ErpUiFactory.BuildFormGrid(
                ("إجمالي الطول من فاتورة الصين", ErpUiFactory.FormField($"{cost.TotalLengthFromInvoice:N0} م")),
                ("وزن الحاوية بالكيلو", ErpUiFactory.FormField($"{cost.ContainerWeightKg:N0}")),
                ("وزن الحاوية بالغرام", ErpUiFactory.FormField($"{cost.ContainerWeightGrams:N0}")),
                ("مبلغ الجمارك المدفوع", ErpUiFactory.FormField($"{cost.CustomsAmountPaid:N0} ر.س")),
                ("تكلفة الجمارك لكل متر", ErpUiFactory.FormField($"{cost.CustomsCostPerMeter:N4} ر.س")),
                ("متوسط وزن المتر بالغرام", ErpUiFactory.FormField($"{cost.AvgGramPerMeter:N2} غرام")),
                ("الشحن", ErpUiFactory.FormField($"{cost.Shipping:N0} ر.س")),
                ("التخليص", ErpUiFactory.FormField($"{cost.Clearance:N0} ر.س")),
                ("مصاريف أخرى", ErpUiFactory.FormField($"{cost.OtherExpenses:N0} ر.س")),
                ("إجمالي مصاريف الاستيراد", ErpUiFactory.FormField($"{cost.TotalImportExpenses:N0} ر.س")),
                ("تكلفة المصاريف لكل متر", ErpUiFactory.FormField($"{cost.ExpenseCostPerMeter:N4} ر.س"))
            );
            stack.Children.Add(ErpUiFactory.Card(form));
            stack.Children.Add(ErpUxFactory.KpiStrip(
                ("تكلفة الجمارك/م", $"{cost.CustomsCostPerMeter:N4} ر.س"),
                ("تكلفة المصاريف/م", $"{cost.ExpenseCostPerMeter:N4} ر.س"),
                ("متوسط غرام/م", $"{cost.AvgGramPerMeter:N2}"),
                ("إجمالي المصاريف", $"{cost.TotalImportExpenses:N0} ر.س")));
            stack.Children.Add(ErpUxFactory.ActionToolbar("Landing Cost", ("اعتماد التكاليف", true), ("طباعة المراجعة", false), ("التالي: اعتماد الحاوية", false)));

            root.Content = stack;
            return Wrap(root);
        }

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
            var uc = new UserControl { Content = content, Background = Br("AppBgBrush") as SolidColorBrush };
            return uc;
        }
    }
}

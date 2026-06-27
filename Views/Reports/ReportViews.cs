using ERPSystem.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ERPSystem.Views.Reports
{
    public static class ReportViews
    {
        private static readonly (string Key, string Title, string Desc)[] Reports =
        [
            ("Financial", "القوائم المالية", "الميزانية وقائمة الدخل"),
            ("Inventory", "تقارير المخزون", "أرصدة الأقمشة والحركات"),
            ("Containers", "تقارير الحاويات", "استيراد الصين والتكاليف"),
            ("Sales", "تقارير المبيعات", "مبيعات الأقمشة حسب الفترة"),
            ("Customers", "تقارير العملاء", "الذمم وكشوف الحساب"),
            ("Suppliers", "تقارير الموردين", "مشتريات ومستحقات"),
            ("BI", "مؤشرات الإدارة", "مؤشرات أداء الجملة"),
        ];

        public static UserControl Create(string key)
        {
            var report = Reports.FirstOrDefault(r => r.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(report.Key))
                return BuildReportScreen(report.Title, report.Desc);
            return BuildHub();
        }

        private static UserControl BuildHub()
        {
            var root = new ScrollViewer { Padding = new Thickness(16), VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle("التقارير"));
            var grid = new UniformGrid { Columns = 3 };
            foreach (var r in Reports)
                grid.Children.Add(ReportCard(r.Title, r.Desc));
            stack.Children.Add(grid);
            root.Content = stack;
            return Wrap(root);
        }

        private static UserControl BuildReportScreen(string title, string desc)
        {
            var root = new ScrollViewer { Padding = new Thickness(16), VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle(title));
            stack.Children.Add(new TextBlock { Text = desc, Foreground = Br("TextSecondaryBrush"), Margin = new Thickness(0, 0, 0, 16) });

            stack.Children.Add(ErpUiFactory.BuildFilterRow(
                ("من تاريخ", new DatePicker { Width = 140, SelectedDate = DateTime.Today.AddMonths(-1) }),
                ("إلى تاريخ", new DatePicker { Width = 140, SelectedDate = DateTime.Today }),
                ("تطبيق", new Button { Content = "تطبيق", Style = S("PrimaryButtonStyle"), Width = 80 })));

            stack.Children.Add(new TextBlock
            {
                Text = "ERP PRO › التقارير › " + title,
                FontSize = 11, Foreground = Br("TextMutedBrush"), Margin = new Thickness(0, 0, 0, 8)
            });

            var kpis = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
            ErpUiFactory.SetSummaryCards(kpis, new[]
            {
                ("إجمالي", "248,500 ر.س", "\uE8C1", B("PrimaryBrush")),
                ("عدد السجلات", "156", "\uE9D2", B("InfoBrush")),
            });
            stack.Children.Add(kpis);

            stack.Children.Add(ErpUiFactory.Card(new Border
            {
                Height = 120,
                Background = Br("SurfaceAltBrush"),
                CornerRadius = new CornerRadius(6),
                Child = new TextBlock
                {
                    Text = "مخططات التقرير — Coming in Database Phase",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Br("TextMutedBrush")
                }
            }));

            stack.Children.Add(new TextBlock { Text = "نتائج التقرير — نمط A4 عربي", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 16, 0, 8) });
            stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildGrid(new[] {
                new { البيان = "مبيعات أقمشة", القيمة = "185,000 ر.س" },
                new { البيان = "تحصيلات", القيمة = "142,000 ر.س" },
            })));

            stack.Children.Add(ErpUxFactory.ExportBar());

            root.Content = stack;
            return Wrap(root);
        }

        private static Border ReportCard(string title, string desc) => ErpUiFactory.Card(new StackPanel
        {
            Children =
            {
                new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, FontSize = 14 },
                new TextBlock { Text = desc, Foreground = Br("TextMutedBrush"), Margin = new Thickness(0, 6, 0, 0) }
            }
        }, new Thickness(0, 0, 12, 12));

        private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
        private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
        private static Style S(string k) => (Style)System.Windows.Application.Current.Resources[k]!;
        private static UserControl Wrap(UIElement c) => new() { Content = c, Background = Br("AppBgBrush") as SolidColorBrush };
    }
}

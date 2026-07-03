using ERPSystem.Core;
using ERPSystem.Views.Reports;
using ERPSystem.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Views.Reports
{
    public static class ReportViews
    {
        public static UserControl Create(string key) => key switch
        {
            "BI" => BuildExecutiveDashboard(),
            "Reports" => ModuleReportsViews.CreateHub(AppModule.Reports),
            _ => ModuleReportsViews.CreateHub(AppModule.Reports)
        };

        private static UserControl BuildExecutiveDashboard()
        {
            var root = new ScrollViewer { Padding = new Thickness(16), VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle("لوحة الإدارة"));
            stack.Children.Add(new TextBlock
            {
                Text = "مؤشرات شاملة عبر الأقسام — للتقارير التفصيلية انتقل إلى قسم كل وحدة › التقارير",
                Foreground = Br("TextSecondaryBrush"),
                Margin = new Thickness(0, 0, 0, 16),
                TextWrapping = TextWrapping.Wrap
            });

            var kpis = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
            ErpUiFactory.SetSummaryCards(kpis, new[]
            {
                ("المبيعات", "—", "\uE8F1", B("PrimaryBrush")),
                ("المخزون", "—", "\uE821", B("AccentInventoryBrush")),
                ("الذمم", "—", "\uE8C1", B("InfoBrush")),
            });
            stack.Children.Add(kpis);
            stack.Children.Add(ErpUxFactory.InfoBanner(
                "التقارير التشغيلية أصبحت داخل كل قسم (مخزون، مبيعات، محاسبة...) كما في Odoo.", "info"));

            root.Content = stack;
            return Wrap(root);
        }

        private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
        private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
        private static UserControl Wrap(UIElement c) => new() { Content = c, Background = Br("AppBgBrush") as SolidColorBrush };
    }
}

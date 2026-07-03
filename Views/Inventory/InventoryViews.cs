using ERPSystem.Controls;
using ERPSystem.Controls.Inventory;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Views.Reports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Views.Inventory
{
    public static class InventoryViews
    {
        public static UserControl Create(string key) => key switch
        {
            "Categories" => BuildCategories(),
            "ImportExcel" => BuildImportExcel(),
            "OpeningStock" => BuildOpeningStock(),
            "Stocktake" => BuildStocktake(),
            "Transfers" => BuildTransfers(),
            "Settings" => BuildSettings(),
            "Reports" => ModuleReportsViews.CreateHub(AppModule.Inventory),
            "Warehouses" => BuildWarehousesHub(),
            _ => BuildWarehousesHub()
        };

        private static UserControl BuildWarehousesHub()
        {
            var tabs = new TabControl { FontFamily = new FontFamily("Segoe UI, Tahoma, Arial") };
            tabs.Items.Add(new TabItem { Header = "مراكز المستودعات", Content = new InventoryWarehouseListPageControl() });
            tabs.Items.Add(new TabItem { Header = "أرصدة الأقمشة", Content = BuildFabricStock() });
            return Wrap(new ScrollViewer { Content = tabs, Padding = new Thickness(16), VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        }

        private static UserControl BuildFabricStock()
        {
            var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle("أرصدة الأقمشة"));
            stack.Children.Add(PlaceholderUi.EmptyMessage(
                "لا توجد أصناف في المخزون",
                "تظهر الأصناف هنا بعد اعتماد الحاويات وترحيلها للمستودع"));
            root.Content = stack;
            return Wrap(root);
        }

        private static UserControl BuildCategories() => Wrap(new InventoryFabricCategoriesPageControl());

        private static UserControl BuildImportExcel()
        {
            var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle("استيراد Excel للمخزون"));
            stack.Children.Add(new TextBlock
            {
                Text = "استيراد أرصدة الأقمشة من ملف",
                Foreground = Br("TextSecondaryBrush"),
                Margin = new Thickness(0, 0, 0, 12)
            });
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            foreach (var a in new[] { "رفع ملف", "معاينة", "تأكيد" })
            {
                var captured = a;
                var btn = new Button { Content = a, Style = S("SecondaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) };
                btn.Click += (_, _) => MockInteractionService.ShowComingSoon(captured);
                actions.Children.Add(btn);
            }
            stack.Children.Add(actions);
            stack.Children.Add(PlaceholderUi.EmptyMessage(
                "لا توجد بيانات للمعاينة",
                "ارفع ملف Excel لعرض الأصناف قبل التأكيد"));
            root.Content = stack;
            return Wrap(root);
        }

        private static UserControl BuildOpeningStock() => FormPage("مواد أول مدة", "إدخال رصيد افتتاحي للمستودع",
            ["حفظ", "اعتماد"],
            ErpUiFactory.BuildFormGrid(
                ("رقم الإدخال", ErpUiFactory.FormField("")),
                ("السنة المالية", ErpUiFactory.FormField("")),
                ("التاريخ", new DatePicker
                {
                    SelectedDate = null,
                    Height = 36,
                    Width = 160,
                    Style = S("EnterpriseDatePickerStyle")
                }),
                ("المستودع", ErpUiFactory.FormField("")),
                ("المسؤول", ErpUiFactory.FormField("")),
                ("مصدر الإدخال", ErpUiFactory.FormField("")),
                ("ملاحظات", ErpUiFactory.FormField(""))));

        private static UserControl BuildStocktake() => FormPage("الجرد", "جلسات الجرد وجرد الحاويات",
            ["جلسة جديدة", "جرد حاوية"],
            PlaceholderUi.EmptyMessage("لا توجد جلسات جرد"));

        private static UserControl BuildTransfers() => FormPage("المناقلات", "نقل الأقمشة بين المستودعات",
            ["مناقلة جديدة"],
            PlaceholderUi.EmptyMessage("لا توجد مناقلات"));

        private static UserControl BuildSettings() => FormPage("إعدادات المخزون", "وحدات القياس، سياسات الجرد، التنبيهات",
            ["حفظ"],
            PlaceholderUi.DevelopmentPhase("إعدادات المخزون"));

        private static UserControl FormPage(string title, string sub, string[] actions, UIElement body)
        {
            var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle(title));
            stack.Children.Add(new TextBlock { Text = sub, Foreground = Br("TextSecondaryBrush"), Margin = new Thickness(0, 0, 0, 12) });
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            foreach (var a in actions)
            {
                var captured = a;
                var btn = new Button { Content = a, Style = S("SecondaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) };
                btn.Click += (_, _) => MockInteractionService.ShowComingSoon(captured);
                row.Children.Add(btn);
            }
            stack.Children.Add(row);
            stack.Children.Add(ErpUiFactory.Card(body));
            root.Content = stack;
            return Wrap(root);
        }

        private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
        private static Style S(string k) => (Style)System.Windows.Application.Current.Resources[k]!;
        private static UserControl Wrap(UIElement c) => new() { Content = c };
    }
}

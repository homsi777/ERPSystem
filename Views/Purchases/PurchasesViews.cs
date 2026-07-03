using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Views.Reports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Views.Purchases
{
    public static class PurchasesViews
    {
        public static UserControl Create(string key) => key switch
        {
            "Orders" => FormPage("أمر شراء", "إنشاء أمر شراء أقمشة"),
            "Returns" => FormPage("مرتجع شراء", "مرتجع فاتورة شراء"),
            "Reports" => ModuleReportsViews.CreateHub(AppModule.Purchases),
            _ => InvoiceList()
        };

        private static UserControl InvoiceList()
        {
            var page = new ErpListModuleControl();
            page.Configure(EntityType.PurchaseInvoice, AppModule.Purchases);
            page.SetHeader("فواتير الشراء", "مشتريات الأقمشة والموردين", "\uE7BF", B("AccentOrdersBrush"));
            page.SetPrimaryButton("فاتورة شراء جديدة");
            page.SetEmptyState("لا توجد فواتير مشتريات", "فاتورة شراء جديدة", "\uE7BF");
            page.WirePrimaryTo(AppModule.Purchases, "Orders");
            page.BindData([]);
            return page;
        }

        private static UserControl FormPage(string title, string sub)
        {
            var root = new ScrollViewer { Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle(title));
            stack.Children.Add(new TextBlock { Text = sub, Margin = new Thickness(0, 0, 0, 12) });
            stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
                ("رقم المستند", ErpUiFactory.FormField("")),
                ("المورد", ErpUiFactory.FormField("")),
                ("التاريخ", new DatePicker { SelectedDate = null, Height = 36, Width = 160 }),
                ("المستودع", ErpUiFactory.FormField("")),
                ("ملاحظات", ErpUiFactory.FormField("")))));
            root.Content = stack;
            return new UserControl { Content = root };
        }

        private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
    }
}

using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Purchases;
using ERPSystem.Helpers;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ERPSystem.Views.Purchases
{
    public static class PurchasesViews
    {
        public static UserControl Create(string key) => key switch
        {
            "Orders" => FormPage("أمر شراء", "إنشاء أمر شراء أقمشة"),
            "Returns" => FormPage("مرتجع شراء", "مرتجع فاتورة شراء"),
            _ => InvoiceList()
        };

        private static UserControl InvoiceList()
        {
            var data = PurchaseSampleData.Generate(30);
            var page = new ErpListModuleControl();
            page.Configure(EntityType.PurchaseInvoice, AppModule.Purchases);
            page.SetHeader("فواتير الشراء", "مشتريات الأقمشة والموردين", "\uE7BF", B("AccentOrdersBrush"));
            page.SetPrimaryButton("فاتورة شراء جديدة");
            page.SetEmptyState("لا توجد فواتير شراء", "فاتورة شراء جديدة", "\uE7BF");
            page.WirePrimaryTo(AppModule.Purchases, "Orders");
            var g = page.Grid;
            g.AutoGenerateColumns = false;
            foreach (var (h, p, w) in new (string, string, object)[] {
                ("رقم الفاتورة","InvoiceNumber",130),("المورد","SupplierName","*"),("التاريخ","InvoiceDate",110),
                ("الإجمالي","TotalAmount",120),("المتبقي","Remaining",110),("الحالة","StatusDisplay",80)
            }) AddCol(g, h, p, w, p is "TotalAmount" or "Remaining" ? "N2" : p == "InvoiceDate" ? "yyyy/MM/dd" : null);
            page.BindData(data);
            return page;
        }

        private static UserControl FormPage(string title, string sub)
        {
            var root = new System.Windows.Controls.ScrollViewer { Padding = new Thickness(16) };
            var stack = new System.Windows.Controls.StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle(title));
            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = sub, Margin = new Thickness(0, 0, 0, 12) });
            stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
                ("رقم المستند", ErpUiFactory.FormField("جديد")),
                ("المورد", ErpUiFactory.FilterCombo(["مورد قوانغتشو"])),
                ("التاريخ", ErpUiFactory.FormDate()),
                ("المستودع", ErpUiFactory.FilterCombo(["المستودع الرئيسي"])),
                ("ملاحظات", ErpUiFactory.FormField("")))));
            root.Content = stack;
            return new UserControl { Content = root };
        }

        private static void AddCol(DataGrid g, string h, string p, object w, string? fmt)
            => ErpUiFactory.AddGridColumn(g, h, p, w, fmt);

        private static SolidColorBrush B(string k) => (SolidColorBrush)Application.Current.Resources[k]!;
    }
}

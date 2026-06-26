using ERPSystem.Controls;
using ERPSystem.Controls.Customers;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Customers;
using ERPSystem.Core.Suppliers;
using ERPSystem.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ERPSystem.Views.Parties
{
    public static class PartyViews
    {
        public static UserControl CreateCustomer(string key) => key switch
        {
            "Form" => PartyForm("عميل", true),
            "Opening" => OpeningBalances("عملاء"),
            "Statement" => Wrap(new CustomerAccountStatementControl()),
            "Invoices" => InvoicesPage("عميل"),
            _ => CustomerList()
        };

        public static UserControl CreateSupplier(string key) => key switch
        {
            "Form" => PartyForm("مورد", false),
            "Statement" => StatementPage("مورد"),
            "Invoices" => InvoicesPage("مورد"),
            _ => SupplierList()
        };

        private static UserControl CustomerList()
        {
            var data = CustomerSampleData.Generate(40);
            var page = new ErpListModuleControl();
            page.Configure(EntityType.Customer, AppModule.Customers);
            page.SetHeader("سجل العملاء", "عملاء الجملة والموزعين", "\uE716", B("AccentCustomersBrush"));
            page.SetPrimaryButton("إضافة عميل");
            page.SetEmptyState("لا يوجد عملاء مسجلون", "إضافة عميل", "\uE716");
            page.WirePrimaryTo(AppModule.Customers, "Form");
            var g = page.Grid;
            g.AutoGenerateColumns = false;
            foreach (var (h, p, w) in new (string, string, object)[] {
                ("الكود","Code",90),("الاسم","NameAr","*"),("المدينة","Region",100),("الهاتف","Phone",120),
                ("الرصيد","Balance",100),("عدد الفواتير","TotalInvoices",90),("الحالة","Status",80)
            }) AddCol(g, h, p, w, p == "Balance" ? "N2" : null);
            page.BindData(data);
            return page;
        }

        private static UserControl SupplierList()
        {
            var data = SupplierSampleData.Generate(20);
            var page = new ErpListModuleControl();
            page.Configure(EntityType.Supplier, AppModule.Suppliers);
            page.SetHeader("سجل الموردين", "موردو الصين والموردون المحليون", "\uE779", B("AccentPayableBrush"));
            page.SetPrimaryButton("إضافة مورد");
            page.SetEmptyState("لا يوجد موردون مسجلون", "إضافة مورد", "\uE779");
            page.WirePrimaryTo(AppModule.Suppliers, "Form");
            var g = page.Grid;
            g.AutoGenerateColumns = false;
            foreach (var (h, p, w) in new (string, string, object)[] {
                ("الكود","Code",90),("الاسم","Name","*"),("المدينة","Country",90),("الهاتف","Phone",120),
                ("الرصيد","Balance",100),("عدد الفواتير","InvoiceCount",90),("الحالة","StatusDisplay",80)
            }) AddCol(g, h, p, w, p == "Balance" ? "N2" : null);
            page.BindData(data);
            return page;
        }

        private static UserControl PartyForm(string type, bool isCustomer)
        {
            var root = new ScrollViewer { Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle($"إضافة / تعديل {type}"));
            stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
                ("الاسم", ErpUiFactory.FormField(isCustomer ? "أحمد الحمصي" : "مورد قوانغتشو")),
                ("الهاتف", ErpUiFactory.FormField("0500000000")),
                ("المدينة", ErpUiFactory.FormField("جدة")),
                ("العنوان", ErpUiFactory.FormField("حي الرويس")),
                ("حد الائتمان", ErpUiFactory.FormField("100000")),
                ("العملة", ErpUiFactory.FormField("ر.س")),
                ("ملاحظات", ErpUiFactory.FormField("")),
                ("الحالة", ErpUiFactory.FilterCombo(["نشط", "معطل"])))));
            root.Content = stack;
            return Wrap(root);
        }

        private static UserControl StatementPage(string party)
        {
            var root = new ScrollViewer { Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle($"كشف حساب {party}"));
            stack.Children.Add(ErpUiFactory.BuildFilterRow(
                ("الطرف", ErpUiFactory.FilterCombo(["أحمد الحمصي", "مؤسسة النسيج"])),
                ("من تاريخ", new DatePicker { SelectedDate = DateTime.Today.AddMonths(-1), Width = 140 }),
                ("إلى تاريخ", new DatePicker { SelectedDate = DateTime.Today, Width = 140 }),
                ("العملة", ErpUiFactory.FilterCombo(["ر.س"])),
                ("الحالة", ErpUiFactory.FilterCombo(["الكل", "مرحّل"]))));
            stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildGrid(new[] {
                new { التاريخ = "2026/06/01", نوع_المستند = "فاتورة", رقم_المستند = "INV-001", البيان = "بيع أقمشة", مدين = "12,500", دائن = "—", الرصيد = "12,500" },
            })));
            var exp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
            foreach (var a in new[] { "طباعة", "PDF", "Excel" })
                exp.Children.Add(new Button { Content = a, Style = S("SecondaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) });
            stack.Children.Add(exp);
            root.Content = stack;
            return Wrap(root);
        }

        private static UserControl InvoicesPage(string party)
        {
            var root = new ScrollViewer { Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle($"كشف فواتير {party}"));
            stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildGrid(new[] {
                new { رقم = "INV-001", التاريخ = "2026/06/01", المبلغ = "12,500", الحالة = "معتمدة" },
            })));
            root.Content = stack;
            return Wrap(root);
        }

        private static UserControl OpeningBalances(string type)
        {
            var root = new ScrollViewer { Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle($"أرصدة افتتاحية — {type}"));
            stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildGrid(new[] {
                new { الكود = "C-001", الاسم = "أحمد", الرصيد_الافتتاحي = "5,000 ر.س" },
            })));
            root.Content = stack;
            return Wrap(root);
        }

        private static void AddCol(DataGrid g, string h, string p, object w, string? fmt)
            => ErpUiFactory.AddGridColumn(g, h, p, w, fmt);

        private static SolidColorBrush B(string k) => (SolidColorBrush)Application.Current.Resources[k]!;
        private static Brush Br(string k) => (Brush)Application.Current.Resources[k]!;
        private static Style S(string k) => (Style)Application.Current.Resources[k]!;
        private static UserControl Wrap(UIElement c) => new() { Content = c };
    }
}

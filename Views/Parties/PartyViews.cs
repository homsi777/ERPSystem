using ERPSystem.Controls;
using ERPSystem.Controls.Customers;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using ERPSystem.Views.Reports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Views.Parties
{
    public static class PartyViews
    {
        public static UserControl CreateCustomer(string key) => key switch
        {
            "Form" => Wrap(new CustomerFormControl()),
            "Opening" => OpeningBalances("عملاء"),
            "Statement" => CreateCustomerStatementView(),
            "Invoices" => CreateCustomerInvoicesView(),
            "Reports" => ModuleReportsViews.CreateHub(AppModule.Customers),
            _ => Wrap(new CustomerListPageControl())
        };

        public static UserControl CreateSupplier(string key) => key switch
        {
            "Form" => PartyForm("مورد"),
            "Statement" => SupplierEmptyContextPage("كشف حساب المورد", "اختر مورداً لعرض كشف حسابه"),
            "Invoices" => SupplierEmptyContextPage("كشف فواتير المورد", "اختر مورداً لعرض فواتيره"),
            "Reports" => ModuleReportsViews.CreateHub(AppModule.Suppliers),
            _ => SupplierList()
        };

        private static UserControl SupplierList()
        {
            var page = new ErpListModuleControl();
            page.Configure(EntityType.Supplier, AppModule.Suppliers);
            page.SetHeader("سجل الموردين", "موردو الصين والموردون المحليون", "\uE779", B("AccentPayableBrush"));
            page.SetPrimaryButton("إضافة مورد");
            page.SetEmptyState("لا يوجد موردون مضافون بعد", "إضافة مورد", "\uE779");
            page.WirePrimaryTo(AppModule.Suppliers, "Form");
            page.BindData([]);
            return page;
        }

        private static UserControl CreateCustomerStatementView()
        {
            var ctrl = new CustomerAccountStatementControl();
            var (id, name) = CustomerNavigationContext.TakeStatementContext();
            if (id is Guid customerId)
                ctrl.Initialize(customerId, name ?? "");
            return Wrap(ctrl);
        }

        private static UserControl CreateCustomerInvoicesView()
        {
            var (id, _) = CustomerNavigationContext.TakeStatementContext();
            if (id is Guid)
            {
                MockInteractionService.Navigate(AppModule.Sales, "Invoices");
                return Wrap(PlaceholderUi.EmptyMessage(
                    "عرض فواتير العميل",
                    "استخدم قائمة فواتير البيع مع البحث عن العميل"));
            }

            return Wrap(PlaceholderUi.EmptyMessage("اختر عميلاً لعرض فواتيره"));
        }

        private static UserControl SupplierEmptyContextPage(string title, string message)
        {
            var root = new ScrollViewer { Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle(title));
            stack.Children.Add(PlaceholderUi.EmptyMessage(message));
            root.Content = stack;
            return Wrap(root);
        }

        private static UserControl PartyForm(string type)
        {
            var root = new ScrollViewer { Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle($"إضافة / تعديل {type}"));
            stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
                ("الاسم", ErpUiFactory.FormField("")),
                ("الهاتف", ErpUiFactory.FormField("")),
                ("المدينة", ErpUiFactory.FormField("")),
                ("العنوان", ErpUiFactory.FormField("")),
                ("حد الائتمان", ErpUiFactory.FormField("")),
                ("العملة", ErpUiFactory.FormField("")),
                ("ملاحظات", ErpUiFactory.FormField("")),
                ("الحالة", ErpUiFactory.FilterCombo([])))));
            root.Content = stack;
            return Wrap(root);
        }

        private static UserControl OpeningBalances(string type)
        {
            var root = new ScrollViewer { Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle($"أرصدة افتتاحية — {type}"));
            stack.Children.Add(PlaceholderUi.EmptyMessage("لا توجد أرصدة افتتاحية"));
            root.Content = stack;
            return Wrap(root);
        }

        private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
        private static UserControl Wrap(UIElement c) => new() { Content = c };
    }
}

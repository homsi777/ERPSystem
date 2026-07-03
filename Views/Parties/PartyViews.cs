using ERPSystem.Controls;
using ERPSystem.Controls.Customers;
using ERPSystem.Controls.Suppliers;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using ERPSystem.Services.Suppliers;
using ERPSystem.Views.Reports;
using System.Windows;
using System.Windows.Controls;

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
            "Form" => Wrap(new SupplierFormControl()),
            "Opening" => Wrap(new SupplierOpeningBalanceControl()),
            "Statement" => CreateSupplierStatementView(),
            "Invoices" => CreateSupplierInvoicesView(),
            "Reports" => ModuleReportsViews.CreateHub(AppModule.Suppliers),
            _ => Wrap(new SupplierListPageControl())
        };

        private static UserControl CreateSupplierStatementView()
        {
            var ctrl = new SupplierAccountStatementControl();
            var (id, name) = SupplierNavigationContext.TakeStatementContext();
            if (id is Guid supplierId)
                ctrl.Initialize(supplierId, name ?? "");
            return Wrap(ctrl);
        }

        private static UserControl CreateSupplierInvoicesView()
        {
            var (id, name) = SupplierNavigationContext.TakeStatementContext();
            if (id is Guid supplierId)
            {
                var list = new SupplierInvoiceListControl();
                list.Initialize(supplierId);
                return Wrap(list);
            }

            return Wrap(PlaceholderUi.EmptyMessage("اختر مورداً لعرض فواتيره"));
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
            var (id, name) = CustomerNavigationContext.TakeStatementContext();
            if (id is Guid customerId)
            {
                var list = new Controls.Sales.SalesInvoiceListPageControl();
                list.ScopeToCustomer(customerId, name ?? "العميل");
                return Wrap(list);
            }

            return Wrap(PlaceholderUi.EmptyMessage("اختر عميلاً لعرض فواتيره"));
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

        private static UserControl Wrap(UIElement c) => new() { Content = c };
    }
}

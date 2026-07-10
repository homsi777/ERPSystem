using ERPSystem.Controls;
using ERPSystem.Controls.Sales;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Views.Reports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Views.Sales
{
    public static class SalesViews
    {
        public static UserControl Create(string key) => key switch
        {
            "NewInvoice" => BuildNewInvoice(),
            "InvoiceView" => BuildInvoiceView(),
            "NewReturn" => BuildReturn(),
            "Returns" => BuildReturnsList(),
            "Delivery" => BuildDelivery(),
            "Detailing" => BuildDetailing(),
            "Reports" => ModuleReportsViews.CreateHub(AppModule.Sales),
            _ => BuildInvoiceList()
        };

        private static UserControl BuildInvoiceList() => new SalesInvoiceListPageControl();

        private static UserControl BuildNewInvoice() => Wrap(new NewSalesInvoiceControl());

        private static UserControl BuildInvoiceView()
        {
            // Legacy submodule entry — redirect users to the real invoice list / operations center flow.
            return BuildInvoiceList();
        }

        private static UserControl BuildDetailing() => new WarehouseDetailingPageControl();

        private static UserControl BuildDelivery() => new SalesDeliveryListPageControl();

        private static FontFamily Ff() => new("Segoe UI, Tahoma, Arial");

        private static UserControl BuildReturn() => new SalesReturnListPageControl();
        private static UserControl BuildReturnsList() => new SalesReturnListPageControl();

        private static UserControl FormSimple(string t, string s)
        {
            var root = new ScrollViewer { Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle(t));
            stack.Children.Add(PlaceholderUi.DevelopmentPhase(s));
            root.Content = stack;
            return Wrap(root);
        }

        private static void AddCol(DataGrid g, string h, string p, object w, string? fmt)
            => ErpUiFactory.AddGridColumn(g, h, p, w, fmt);

        private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
        private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
        private static Style S(string k) => (Style)System.Windows.Application.Current.Resources[k]!;
        private static UserControl Wrap(UIElement c) => new() { Content = c };
    }
}

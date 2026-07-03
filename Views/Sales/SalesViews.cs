using ERPSystem.Controls;
using ERPSystem.Controls.Sales;
using ERPSystem.Controls.Workspace;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Sales;
using ERPSystem.Core.Workspace;
using ERPSystem.Helpers;
using ERPSystem.Views.Reports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ERPSystem.Views.Sales
{
    public enum FabricInvoiceWorkflowStatus
    {
        Draft, AwaitingDetailing, Detailed, Approved, ReadyForDelivery, Delivered, Cancelled
    }

    public class FabricSalesInvoiceRow
    {
        public SalesInvoice? Source { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string Warehouse { get; set; } = "";
        public string Container { get; set; } = "";
        public int RollCount { get; set; }
        public decimal Amount { get; set; }
        public FabricInvoiceWorkflowStatus WorkflowStatus { get; set; }
        public DateTime Date { get; set; }

        public string StatusDisplay => WorkflowStatus switch
        {
            FabricInvoiceWorkflowStatus.Draft => "مسودة",
            FabricInvoiceWorkflowStatus.AwaitingDetailing => "بانتظار التفصيل",
            FabricInvoiceWorkflowStatus.Detailed => "مفصلة",
            FabricInvoiceWorkflowStatus.Approved => "معتمدة",
            FabricInvoiceWorkflowStatus.ReadyForDelivery => "جاهزة للتسليم",
            FabricInvoiceWorkflowStatus.Delivered => "مسلمة",
            FabricInvoiceWorkflowStatus.Cancelled => "ملغاة",
            _ => ""
        };
    }

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
            var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle("عرض فاتورة بيع"));
            stack.Children.Add(PlaceholderUi.EmptyMessage(
                "يرجى اختيار فاتورة من قائمة فواتير البيع لعرضها",
                "افتح قائمة الفواتير ثم اختر فاتورة لفتح مركز العمليات"));
            root.Content = stack;
            return Wrap(root);
        }

        private static UserControl BuildDetailing() => new WarehouseDetailingPageControl();

        private static UserControl BuildDelivery()
        {
            var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle("التسليم — بطاقة تسليم العميل"));
            stack.Children.Add(PlaceholderUi.DatabasePhase("بطاقة التسليم وإذن التسليم"));
            stack.Children.Add(ErpUxFactory.InfoBanner(
                "قسم التسليم غير مفعّل بعد. بعد اعتماد الفاتورة وإكمال التفصيل، ستُنشأ هنا بطاقة التسليم للعميل.",
                "info"));
            root.Content = stack;
            return Wrap(root);
        }

        private static FontFamily Ff() => new("Segoe UI, Tahoma, Arial");

        private static UserControl BuildReturn() => FormSimple("مرتجع بيع جديد", "إنشاء مرتجع من فاتورة بيع");
        private static UserControl BuildReturnsList() => FormSimple("قائمة مرتجعات البيع", "مرتجعات البيع المعتمدة");

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

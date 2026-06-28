using ERPSystem.Controls;
using ERPSystem.Controls.Sales;
using ERPSystem.Controls.Workspace;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Sales;
using ERPSystem.Core.Workspace;
using ERPSystem.Helpers;
using ERPSystem.Services;
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
        public string Warehouse { get; set; } = "المستودع الرئيسي";
        public string Container { get; set; } = "CN-2026-001";
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
            _ => BuildInvoiceList()
        };

        private static UserControl BuildInvoiceList() => new SalesInvoiceListPageControl();

        private static UserControl BuildNewInvoice() => Wrap(new NewSalesInvoiceControl());

        private static UserControl BuildInvoiceView() => InvoiceForm("عرض فاتورة بيع");

        private static UserControl InvoiceForm(string title)
        {
            var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle(title));
            stack.Children.Add(ErpUxFactory.WorkflowStepper(
                ("اختيار العميل", true, true),
                ("المستودع والحاوية", true, false),
                ("اختيار القماش", false, false),
                ("عدد الأثواب", false, false),
                ("حفظ مسودة", false, false),
                ("تفصيل المستودع", false, false),
                ("اعتماد", false, false)));
            stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
                ("رقم الفاتورة", ErpUiFactory.FormField("INV-2026-0100")),
                ("التاريخ", ErpUiFactory.FormDate()),
                ("العميل", ErpUiFactory.FilterCombo(["أحمد الحمصي", "مؤسسة النسيج"])),
                ("الحاوية", ErpUiFactory.FilterCombo(["CN-2026-001", "CN-2026-012", "CN-2026-018"])),
                ("المستودع", ErpUiFactory.FilterCombo(["المستودع الرئيسي"])),
                ("نوع الدفع", ErpUiFactory.FilterCombo(["نقدي", "آجل"])),
                ("مبلغ الآجل", ErpUiFactory.FormField("0")),
                ("العملة", ErpUiFactory.FormField("ر.س")),
                ("ملاحظات", ErpUiFactory.FormField("")))));

            stack.Children.Add(ErpUiFactory.SectionTitle("سطور الفاتورة — اختيار القماش والأثواب"));
            var lineForm = ErpUiFactory.BuildFormGrid(
                ("نوع القماش", ErpUiFactory.FilterCombo(["كولومبيا", "تركي", "صيني"])),
                ("كود التوب", ErpUiFactory.FormField("COL-01")),
                ("اللون", ErpUiFactory.FilterCombo(["أبيض", "بيج", "أسود"])),
                ("عدد الأثواب", ErpUiFactory.FormField("5")),
                ("سعر المتر", ErpUiFactory.FormField("45")),
                ("ملاحظة", ErpUiFactory.FormField("")));
            stack.Children.Add(ErpUiFactory.Card(lineForm));
            var lines = ErpUiFactory.BuildGrid(new[] {
                new { نوع_القماش = "كولومبيا", كود_التوب = "COL-01", اللون = "أبيض", عدد_الأثواب = 5, الطول = "بانتظار التفصيل", الوحدة = "متر", سعر_الوحدة = 45, الإجمالي = "—" },
            }, false);
            stack.Children.Add(ErpUiFactory.Card(lines));
            var saveBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
            saveBar.Children.Add(new Button { Content = "حفظ مسودة", Style = S("PrimaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) });
            saveBar.Children.Add(new Button { Content = "إرسال للمستودع — بانتظار التفصيل", Style = S("SecondaryButtonStyle") });
            stack.Children.Add(saveBar);
            stack.Children.Add(new TextBlock
            {
                Text = "قاعدة العمل: لا يُعتمد إجمالي الفاتورة حتى إدخال أطوال الأثواب. البيع بالمتر.",
                Foreground = Br("WarningBrush"), Margin = new Thickness(0, 12, 0, 0), TextWrapping = TextWrapping.Wrap
            });
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
            stack.Children.Add(new TextBlock { Text = s, Margin = new Thickness(0, 0, 0, 12) });
            stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildGrid(new[] { new { رقم = "RET-001", فاتورة = "INV-001", العميل = "أحمد", المبلغ = "2,500 ر.س" } })));
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

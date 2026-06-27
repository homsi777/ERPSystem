using ERPSystem.Controls;
using ERPSystem.Controls.Sales;
using ERPSystem.Controls.Workspace;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Sales;
using ERPSystem.Core.Workspace;
using ERPSystem.Helpers;
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
        private static List<FabricSalesInvoiceRow> _invoices = BuildInvoices();

        public static UserControl Create(string key) => key switch
        {
            "NewInvoice" => BuildNewInvoice(),
            "InvoiceView" => BuildInvoiceView(),
            "NewReturn" => BuildReturn(),
            "Returns" => BuildReturnsList(),
            "Delivery" => BuildDelivery(),
            _ => BuildInvoiceList()
        };

        private static List<FabricSalesInvoiceRow> BuildInvoices()
        {
            var src = SalesSampleData.Generate(40);
            var rnd = new Random(3);
            return src.Select((inv, i) => new FabricSalesInvoiceRow
            {
                Source = inv,
                InvoiceNumber = inv.InvoiceNumber,
                CustomerName = inv.CustomerNameAr,
                RollCount = rnd.Next(3, 25),
                Amount = inv.GrandTotal,
                Date = inv.Date,
                WorkflowStatus = (FabricInvoiceWorkflowStatus)(i % 7)
            }).ToList();
        }

        private static UserControl BuildInvoiceList()
        {
            var page = new ErpListModuleControl();
            page.Configure(EntityType.SalesInvoice, AppModule.Sales);
            page.SetHeader("فواتير البيع", "إدارة فواتير بيع الأقمشة — التفصيل بالمتر", "\uE8F1", B("AccentSalesBrush"));
            page.SetPrimaryButton("فاتورة بيع جديدة");
            page.SetEmptyState("لا توجد فواتير بيع", "فاتورة بيع جديدة", "\uE9F9");
            page.WirePrimaryTo(AppModule.Sales, "NewInvoice");

            var statusFilter = ErpUiFactory.FilterCombo(
                ["كل الحالات", "مسودة", "بانتظار التفصيل", "مفصلة", "معتمدة", "جاهزة للتسليم", "مسلمة", "ملغاة"], 140);
            statusFilter.SelectionChanged += (_, _) =>
            {
                var sel = statusFilter.SelectedItem?.ToString() ?? "كل الحالات";
                if (sel == "كل الحالات")
                {
                    page.SetExtraFilter(null);
                    page.SetFilterSummary("");
                }
                else
                {
                    page.SetExtraFilter(o => o is FabricSalesInvoiceRow r && r.StatusDisplay == sel);
                    page.SetFilterSummary($"الحالة: {sel}");
                }
            };
            page.SetFilterExtras(statusFilter);

            var g = page.Grid;
            g.AutoGenerateColumns = false;
            foreach (var (h, p, w) in new (string, string, object)[] {
                ("رقم الفاتورة","InvoiceNumber",120),("العميل","CustomerName","*"),("المستودع","Warehouse",110),
                ("الحاوية","Container",110),("الأثواب","RollCount",70),("المبلغ","Amount",100),
                ("الحالة","StatusDisplay",120),("التاريخ","Date",100)
            }) AddCol(g, h, p, w, p == "Amount" ? "N2" : p == "Date" ? "yyyy/MM/dd" : null);
            page.BindData(_invoices.Cast<object>().ToList());
            return page;
        }

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

        private static UserControl BuildDelivery()
        {
            var awaiting = _invoices.Where(i => i.WorkflowStatus == FabricInvoiceWorkflowStatus.AwaitingDetailing).ToList();
            var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle("مهام المستودع — تفصيل الأطوال"));
            stack.Children.Add(ErpUxFactory.InfoBanner(
                $"لديك {awaiting.Count} فاتورة بانتظار التفصيل. اختر فاتورة أو ابدأ الإدخال السريع أدناه.", "warning"));

            if (awaiting.Count > 0)
            {
                var first = awaiting[0];
                var detailing = new WarehouseDetailingWorkspaceControl();
                detailing.LoadInvoice(first.InvoiceNumber, first.CustomerName, first.Container, first.RollCount);
                stack.Children.Add(ErpUiFactory.Card(detailing));
            }
            else
            {
                stack.Children.Add(ErpUiFactory.Card(new TextBlock
                {
                    Text = "لا توجد فواتير بانتظار التفصيل حالياً.",
                    Foreground = Br("TextSecondaryBrush"), Margin = new Thickness(8)
                }));
            }

            stack.Children.Add(ErpUiFactory.SectionTitle("قائمة الانتظار"));
            var g = ErpUiFactory.BuildGrid(awaiting, false);
            AddCol(g, "رقم الفاتورة", "InvoiceNumber", 120, null);
            AddCol(g, "العميل", "CustomerName", 150, null);
            AddCol(g, "الحاوية", "Container", 110, null);
            AddCol(g, "الأثواب", "RollCount", 70, null);
            AddCol(g, "الحالة", "StatusDisplay", 120, null);
            stack.Children.Add(ErpUiFactory.Card(g));
            root.Content = stack;
            return Wrap(root);
        }

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

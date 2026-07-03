using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Controls.Workspace;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Sales;
using ERPSystem.Core.Workspace;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Sales;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Sales;

public sealed class SalesInvoiceOperationsCenterControl : UserControl
{
    private readonly TextBlock _loading = new()
    {
        Text = "جاري تحميل مركز عمليات الفاتورة...",
        Margin = new Thickness(24),
        FontSize = 14,
        Foreground = Brushes.Gray
    };

    private Guid _invoiceId;
    private string _initialTab = "Overview";

    public SalesInvoiceOperationsCenterControl()
    {
        Content = _loading;
    }

    public void Initialize(Guid invoiceId, string initialTab = "Overview")
    {
        _invoiceId = invoiceId;
        _initialTab = initialTab;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (!AppServices.IsInitialized)
            return;

        var result = await SalesUiService.Instance.GetOperationsCenterAsync(_invoiceId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
        {
            Content = new TextBlock
            {
                Text = "تعذّر تحميل بيانات الفاتورة.",
                Margin = new Thickness(24),
                TextWrapping = TextWrapping.Wrap
            };
            return;
        }

        Content = BuildShell(result.Value);
    }

    private UserControl BuildShell(SalesInvoiceOperationsCenterDto data)
    {
        var invoice = data.Invoice;
        var status = StatusDisplay(invoice.Status);
        var rollCount = invoice.Lines.Sum(l => l.RollCount);
        var unitPrice = invoice.Lines.FirstOrDefault()?.UnitPrice ?? 0m;

        UIElement detailingContent;
        if (data.Detailing is not null)
        {
            var detailing = new WarehouseDetailingWorkspaceControl();
            detailing.LoadFromDatabase(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.CustomerName,
                "—",
                data.Detailing.Rolls,
                unitPrice);
            detailingContent = WrapDetailing(detailing);
        }
        else
        {
            detailingContent = new TextBlock
            {
                Text = "لا توجد مهمة تفصيل مفتوحة لهذه الفاتورة.",
                Margin = new Thickness(16),
                TextWrapping = TextWrapping.Wrap
            };
        }

        var overviewLines = invoice.Lines.Select(l => new
        {
            قماش = l.FabricDisplayName,
            كود = l.FabricCode,
            لون = l.ColorDisplayName,
            أثواب = l.RollCount,
            سعر_المتر = l.UnitPrice,
            الإجمالي = l.LineTotal
        }).ToArray();

        return OperationsCenterShell.Build(new OperationsCenterSpec
        {
            Title = invoice.InvoiceNumber,
            Subtitle = "مركز عمليات فاتورة البيع — بيانات PostgreSQL",
            Breadcrumb = "ERP PRO › المبيعات › فاتورة",
            IconGlyph = "\uE9F9",
            Accent = Br("AccentSalesBrush"),
            AccentLight = Br("PrimaryVeryLightBrush"),
            StatusBadge = status,
            StatusBadgeBackground = invoice.Status == SalesInvoiceStatus.AwaitingDetailing
                ? Br("WarningBgBrush")
                : Br("SuccessBgBrush"),
            StatusBadgeForeground = invoice.Status == SalesInvoiceStatus.AwaitingDetailing
                ? Br("WarningBrush")
                : Br("SuccessBrush"),
            HeaderFields =
            [
                ("العميل", invoice.CustomerName),
                ("تاريخ الفاتورة", invoice.InvoiceDate.ToString("yyyy/MM/dd")),
                ("نوع الدفع", invoice.PaymentType == PaymentType.Cash ? "نقدي" : "آجل"),
                ("إجمالي الأثواب", rollCount.ToString()),
            ],
            Kpis =
            [
                ("الأثواب", rollCount.ToString(), "\uE7C3"),
                ("الإجمالي", $"{invoice.GrandTotal:N2}", "\uE8C1"),
                ("الحالة", status, "\uE9F9"),
                ("سطور", invoice.Lines.Count.ToString(), "\uE8C8"),
            ],
            Workflow =
            [
                ("مسودة", true, invoice.Status != SalesInvoiceStatus.Draft),
                ("بانتظار التفصيل", invoice.Status >= SalesInvoiceStatus.AwaitingDetailing, invoice.Status == SalesInvoiceStatus.AwaitingDetailing),
                ("اكتمل التفصيل", invoice.Status >= SalesInvoiceStatus.Detailed, invoice.Status == SalesInvoiceStatus.Detailed),
                ("معتمدة", invoice.Status >= SalesInvoiceStatus.Approved, invoice.Status == SalesInvoiceStatus.Approved),
            ],
            Tabs =
            [
                Tab("Overview", "تفاصيل الفاتورة", PlaceholderUi.MockGrid(overviewLines)),
                Tab("Detailing", "تفصيل المستودع", detailingContent),
                Tab("Attachments", "مرفقات", PlaceholderUi.DatabasePhase("مرفقات الفاتورة")),
                Tab("Timeline", "الخط الزمني", PlaceholderUi.TabContent("سجل الفاتورة")),
            ],
            QuickActions =
            [
                Q("تعديل", data.CanSendToWarehouse, "Overview", actionKey: "nav:Sales:NewInvoice"),
                Q("اعتماد", data.CanApprove, null, actionKey: "ws:ApproveInvoice"),
                Q("طباعة", false, null, actionKey: "preview:فاتورة البيع"),
            ],
            InitialTabIndex = ResolveTabIndex(_initialTab, "Overview", "Detailing", "Attachments", "Timeline"),
            Context = new OperationsCenterContext
            {
                EntityType = EntityType.SalesInvoice,
                EntityRow = SalesInvoiceListRow.FromDto(invoice, "—", "—"),
                SourceModule = AppModule.Sales,
                Title = invoice.InvoiceNumber
            }
        });
    }

    private static UIElement WrapDetailing(WarehouseDetailingWorkspaceControl ctrl)
    {
        var stack = new StackPanel();
        stack.Children.Add(ErpUxFactory.InfoBanner(
            "أدخل الطول الفعلي لكل توب. لا يُعتمد إجمالي الفاتورة قبل إكمال التفصيل.",
            "warning"));
        stack.Children.Add(ctrl);
        return stack;
    }

    private static string StatusDisplay(SalesInvoiceStatus status) => status switch
    {
        SalesInvoiceStatus.Draft => "مسودة",
        SalesInvoiceStatus.AwaitingDetailing => "بانتظار التفصيل",
        SalesInvoiceStatus.Detailed => "مفصلة",
        SalesInvoiceStatus.ReadyForApproval => "جاهزة للاعتماد",
        SalesInvoiceStatus.Approved => "معتمدة",
        SalesInvoiceStatus.Printed => "مطبوعة",
        SalesInvoiceStatus.Delivered => "مسلمة",
        SalesInvoiceStatus.Cancelled => "ملغاة",
        _ => status.ToString()
    };

    private static int ResolveTabIndex(string selected, params string[] keys)
    {
        for (var i = 0; i < keys.Length; i++)
            if (keys[i].Equals(selected, StringComparison.OrdinalIgnoreCase))
                return i;
        return 0;
    }

    private static OperationsCenterTab Tab(string key, string label, UIElement content) =>
        new() { Key = key, Label = label, Content = content };

    private static OperationsCenterQuickAction Q(
        string label, bool primary, string? tab,
        bool destructive = false, bool confirm = false, string? actionKey = null) =>
        new()
        {
            Label = label,
            Primary = primary,
            TabKey = tab,
            Destructive = destructive,
            RequiresConfirmation = confirm,
            ActionKey = actionKey
        };

    private static SolidColorBrush Br(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
}

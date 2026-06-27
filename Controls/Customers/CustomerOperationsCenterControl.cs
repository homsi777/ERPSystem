using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Customers;
using DomainCustomerStatus = ERPSystem.Domain.Enums.CustomerStatus;
using ERPSystem.Core.Workspace;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Customers;

public sealed class CustomerOperationsCenterControl : UserControl
{
    private readonly ContentPresenter _host = new();
    private readonly TextBlock _loading = new()
    {
        Text = "جاري تحميل مركز عمليات العميل...",
        Margin = new Thickness(24),
        FontSize = 14,
        Foreground = Brushes.Gray
    };

    private Guid _customerId;
    private string _initialTab = "Overview";

    public CustomerOperationsCenterControl()
    {
        Content = _loading;
    }

    public void Initialize(Guid customerId, string initialTab)
    {
        _customerId = customerId;
        _initialTab = initialTab;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (!AppServices.IsInitialized)
            return;

        var result = await CustomerUiService.Instance.GetOperationsCenterAsync(_customerId);
        if (!ApplicationResultPresenter.Present(result))
        {
            Content = new TextBlock { Text = "تعذّر تحميل بيانات العميل.", Margin = new Thickness(24) };
            return;
        }

        var data = result.Value!;
        var row = CustomerListRow.FromDetails(data.Customer);
        Content = BuildShell(data, row, _initialTab);
    }

    private static UserControl BuildShell(CustomerOperationsCenterDto data, CustomerListRow row, string initialTab)
    {
        var c = data.Customer;
        var accent = Br("AccentCustomersBrush");
        var isActive = c.IsActive && c.Status == DomainCustomerStatus.Active;

        var statement = new CustomerAccountStatementControl();
        statement.Initialize(c.Id, c.NameAr);

        return OperationsCenterShell.Build(new OperationsCenterSpec
        {
            Title = c.NameAr,
            Subtitle = "مركز عمليات العميل — بيانات PostgreSQL",
            Breadcrumb = "ERP PRO › العملاء › مركز العمليات",
            IconGlyph = "\uE716",
            Accent = accent,
            AccentLight = Br("PrimaryVeryLightBrush"),
            StatusBadge = row.StatusDisplay,
            StatusBadgeBackground = isActive ? Br("SuccessBgBrush") : Br("WarningBgBrush"),
            StatusBadgeForeground = isActive ? Br("SuccessBrush") : Br("WarningBrush"),
            HeaderFields =
            [
                ("كود العميل", c.Code),
                ("حد الائتمان", $"{c.CreditLimit:N0} ر.س"),
                ("الرصيد الحالي", $"{c.Balance:N0} ر.س"),
                ("فواتير مفتوحة", data.OpenInvoicesCount.ToString()),
                ("الهاتف", c.Phone ?? "—"),
                ("نوع العميل", row.TypeDisplay),
                ("أيام السداد", c.PaymentTermsDays.ToString()),
                ("إجمالي المستحق", $"{data.TotalOutstanding:N0} ر.س"),
            ],
            Kpis =
            [
                ("الرصيد الحالي", $"{c.Balance:N0} ر.س", "\uE8C1"),
                ("فواتير غير مغلقة", data.OpenInvoicesCount.ToString(), "\uE9F9"),
                ("إجمالي المستحق", $"{data.TotalOutstanding:N0} ر.س", "\uE8F1"),
                ("سندات قبض معلقة", data.PendingReceiptsCount.ToString(), "\uE7BF"),
            ],
            Tabs =
            [
                Tab("Overview", "نظرة عامة", OverviewTab(data)),
                Tab("Statement", "كشف الحساب", statement),
                Tab("Invoices", "الفواتير", PlaceholderUi.DatabasePhase("فواتير العميل — تُربط مع وحدة المبيعات")),
                Tab("Receipts", "سندات القبض", PlaceholderUi.DatabasePhase("سندات القبض — تُربط مع المحاسبة")),
            ],
            QuickActions =
            [
                Q("فاتورة جديدة", true, null, actionKey: "ws:NewInvoice"),
                Q("سند قبض", false, null, actionKey: "nav:Accounting:Receipts"),
                Q("كشف حساب", false, "Statement"),
                Q("تعديل", false, null, actionKey: "form:EditCustomer"),
                Q("تعطيل", false, null, destructive: true, confirm: true, actionKey: "ws:DeactivateCustomer"),
            ],
            InitialTabIndex = ResolveTabIndex(initialTab, "Overview", "Statement", "Invoices", "Receipts"),
            Context = new OperationsCenterContext
            {
                EntityType = EntityType.Customer,
                EntityRow = row,
                SourceModule = AppModule.Customers,
                Title = c.NameAr
            }
        });
    }

    private static UIElement OverviewTab(CustomerOperationsCenterDto data)
    {
        var c = data.Customer;
        return PlaceholderUi.MockGrid(new[]
        {
            new { المؤشر = "الرصيد الحالي", القيمة = $"{c.Balance:N0} ر.س" },
            new { المؤشر = "حد الائتمان", القيمة = $"{c.CreditLimit:N0} ر.س" },
            new { المؤشر = "فواتير مفتوحة", القيمة = data.OpenInvoicesCount.ToString() },
            new { المؤشر = "إجمالي المستحق", القيمة = $"{data.TotalOutstanding:N0} ر.س" },
        });
    }

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

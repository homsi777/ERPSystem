using ERPSystem.Application.DTOs.Suppliers;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Suppliers;
using ERPSystem.Core.Workspace;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Suppliers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Suppliers;

public sealed class SupplierOperationsCenterControl : UserControl
{
    private Guid _supplierId;
    private string _initialTab = "Overview";

    public SupplierOperationsCenterControl()
    {
        Content = new TextBlock { Text = "جاري تحميل مركز عمليات المورد...", Margin = new Thickness(24) };
    }

    public void Initialize(Guid supplierId, string initialTab = "Overview")
    {
        _supplierId = supplierId;
        _initialTab = initialTab;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (!AppServices.IsInitialized)
            return;

        var result = await SupplierUiService.Instance.GetOperationsCenterAsync(_supplierId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
        {
            Content = PlaceholderUi.EmptyMessage("تعذّر تحميل بيانات المورد");
            return;
        }

        var data = result.Value;
        var row = SupplierListRow.FromDetails(data.Supplier);
        Content = BuildShell(data, row, _initialTab);
    }

    private static UserControl BuildShell(SupplierOperationsCenterDto data, SupplierListRow row, string initialTab)
    {
        var s = data.Supplier;
        var statement = new SupplierAccountStatementControl();
        statement.Initialize(s.Id, s.NameAr);
        var invoices = new SupplierInvoiceListControl();
        invoices.Initialize(s.Id);

        var paymentsGrid = ErpUiFactory.Card(ErpUiFactory.BuildGrid(
            data.RecentPayments.Select(p => new
            {
                رقم_السند = p.VoucherNumber,
                التاريخ = p.VoucherDate.ToString("yyyy/MM/dd"),
                المبلغ = p.Amount,
                الحالة = p.StatusDisplay
            }).ToArray(), false));

        var notesBox = new TextBox
        {
            Text = s.Notes ?? "",
            AcceptsReturn = true,
            Height = 120,
            TextWrapping = TextWrapping.Wrap
        };
        var notesPanel = new StackPanel();
        notesPanel.Children.Add(notesBox);
        var saveNotesBtn = new Button
        {
            Content = "حفظ الملاحظات",
            Style = (Style)System.Windows.Application.Current.Resources["PrimaryButtonStyle"]!,
            Margin = new Thickness(0, 8, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        saveNotesBtn.Click += async (_, _) =>
        {
            var result = await SupplierUiService.Instance.UpdateAsync(new Application.Commands.Suppliers.UpdateSupplierCommand
            {
                SupplierId = s.Id,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                Phone = s.Phone,
                Email = s.Email,
                Address = s.Address,
                Country = s.Country,
                City = s.City,
                CurrencyCode = s.CurrencyCode,
                PaymentTermsDays = s.PaymentTermsDays,
                CreditLimit = s.CreditLimit,
                TaxNumber = s.TaxNumber,
                PayablesAccountId = s.PayablesAccountId,
                Notes = notesBox.Text.Trim()
            });
            if (ApplicationResultPresenter.Present(result))
                MockInteractionService.ShowSuccess("تم حفظ الملاحظات.");
        };
        notesPanel.Children.Add(saveNotesBtn);

        return OperationsCenterShell.Build(new OperationsCenterSpec
        {
            Title = s.NameAr,
            Subtitle = "مركز عمليات المورد — بيانات PostgreSQL",
            Breadcrumb = "الأمل.AB › الموردون › مركز العمليات",
            IconGlyph = "\uE779",
            Accent = Br("AccentPayableBrush"),
            AccentLight = Br("WarningBgBrush"),
            StatusBadge = row.StatusDisplay,
            StatusBadgeBackground = row.IsActive ? Br("SuccessBgBrush") : Br("WarningBgBrush"),
            StatusBadgeForeground = row.IsActive ? Br("SuccessBrush") : Br("WarningBrush"),
            HeaderFields =
            [
                ("كود المورد", s.Code),
                ("الرصيد المستحق", $"{s.Balance:N2} $"),
                ("شروط السداد", s.PaymentTermsDisplay),
                ("حد الائتمان", $"{s.CreditLimit:N2} $"),
                ("الدولة", s.Country ?? "—"),
                ("الهاتف", s.Phone ?? "—"),
                ("حساب الذمم", s.PayablesAccountName ?? "—"),
                ("آخر حركة", data.LastTransactionDate?.ToString("yyyy/MM/dd") ?? "—"),
            ],
            Kpis =
            [
                ("مشتريات السنة", $"{data.PurchasesYtd:N0} $", "\uE7BF"),
                ("رصيد مستحق", $"{data.OutstandingBalance:N2} $", "\uE8C1"),
                ("متأخرات", $"{data.OverdueAmount:N2} $", "\uE823"),
                ("فواتير مفتوحة", data.OpenInvoicesCount.ToString(), "\uE9F9"),
            ],
            Tabs =
            [
                Tab("Overview", "نظرة عامة", OverviewTab(data)),
                Tab("Statement", "كشف الحساب", statement),
                Tab("Invoices", "الفواتير", invoices),
                Tab("Payments", "المدفوعات", paymentsGrid),
                Tab("Notes", "ملاحظات", notesPanel),
            ],
            QuickActions =
            [
                Q("سند دفع", true, null, actionKey: "nav:Accounting:Payments"),
                Q("كشف حساب", false, "Statement"),
                Q("تعديل", false, null, actionKey: "form:EditSupplier"),
                Q("تعطيل", false, null, destructive: true, confirm: true, actionKey: "ws:DeactivateSupplier"),
            ],
            InitialTabIndex = ResolveTabIndex(initialTab, "Overview", "Statement", "Invoices", "Payments", "Notes"),
            Context = new OperationsCenterContext
            {
                EntityType = EntityType.Supplier,
                EntityRow = row,
                SourceModule = AppModule.Suppliers,
                Title = s.NameAr
            }
        });
    }

    private static UIElement OverviewTab(SupplierOperationsCenterDto data) =>
        PlaceholderUi.MockGrid(new[]
        {
            new { المؤشر = "مشتريات السنة", القيمة = $"{data.PurchasesYtd:N2} $" },
            new { المؤشر = "الرصيد المستحق", القيمة = $"{data.OutstandingBalance:N2} $" },
            new { المؤشر = "المتأخرات", القيمة = $"{data.OverdueAmount:N2} $" },
            new { المؤشر = "فواتير مفتوحة", القيمة = data.OpenInvoicesCount.ToString() },
        });

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

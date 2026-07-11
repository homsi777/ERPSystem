using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Capital;
using ERPSystem.Diagnostics.Performance;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Capital;

public sealed class CapitalOperationsCenterControl : UserControl
{
    private TabControl? _tabs;

    public CapitalOperationsCenterControl()
    {
        Content = new TextBlock { Text = "جاري التحميل...", Margin = new Thickness(24) };
    }

    public void Initialize(Guid partnerId, string? initialTab = null)
    {
        Loaded += async (_, _) =>
        {
            await LoadAsync(partnerId);
            if (_tabs != null && !string.IsNullOrWhiteSpace(initialTab))
                SelectTab(initialTab);
        };
    }

    private async Task LoadAsync(Guid partnerId)
    {
        if (!AppServices.IsInitialized) return;
        using var perfScope = ScreenLoadProfiler.Begin("Capital.OperationsCenter");
        var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => CapitalPartnerUiService.Instance.GetOperationsCenterAsync(partnerId));
        perfScope?.IncrementServiceCalls();
        if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;

        var shell = BuildShell(result.Value);
        Content = shell;
        _tabs = FindTabs(shell);
    }

    private static UserControl BuildShell(CapitalOperationsCenterDto oc)
    {
        var d = oc.Details;
        var f = oc.Financial;
        return OperationsCenterShell.Build(new OperationsCenterSpec
        {
            Title = d.FullName,
            Subtitle = $"مركز عمل الشريك — {d.Code}",
            Breadcrumb = "الأمل.AB › رأس المال والشركاء › مركز العمل",
            IconGlyph = "\uE8F1",
            Accent = Br("PrimaryBrush"),
            AccentLight = Br("InfoBgBrush"),
            StatusBadge = d.StatusDisplay,
            HeaderFields =
            [
                ("الكود", d.Code),
                ("الهوية", d.NationalId ?? "—"),
                ("الهاتف", d.Phone ?? "—"),
                ("رأس المال", $"{f.CurrentCapitalBase:N2} {f.BaseCurrency}"),
                ("استثمارات", $"{f.TotalInvestmentsBase:N2}"),
                ("سحوبات", $"{f.TotalWithdrawalsBase:N2}"),
            ],
            Kpis =
            [
                ("رأس المال", $"{f.CurrentCapitalBase:N2}", "\uE8C1"),
                ("استثمارات", $"{f.TotalInvestmentsBase:N2}", "\uE710"),
                ("سحوبات", $"{f.TotalWithdrawalsBase:N2}", "\uE719"),
                ("أرباح موزعة", $"{f.DistributedProfitBase:N2}", "\uE9D2"),
                ("معاملات", f.TransactionCount.ToString(), "\uE8A5"),
                ("مشاركات", f.ParticipationCount.ToString(), "\uE8AB"),
            ],
            Tabs =
            [
                Tab("Overview", "نظرة عامة", () => OverviewTab(d, oc)),
                Tab("Financial", "الملخص المالي", () => FinancialTab(f)),
                Tab("Participations", "المشاركات", () => ParticipationsTab(d)),
                Tab("Ledger", "دفتر الاستثمار", () => LedgerTab(d)),
                Tab("Audit", "سجل التدقيق", () => AuditTab(oc.RecentAudit)),
                Tab("Timeline", "الخط الزمني", () => TimelineTab(oc.Timeline)),
                Tab("Notes", "ملاحظات", () => NotesTab(d)),
                Tab("FutureAccounting", "المحاسبة المستقبلية", () => FutureTab()),
            ],
            QuickActions =
            [
                Q("تعديل", false, null, actionKey: "nav:CapitalPartners:Form"),
                Q("استثمار", false, null, actionKey: "capital:investment"),
                Q("سحب", false, null, actionKey: "capital:withdrawal"),
                Q("أرشفة", false, null, destructive: true, confirm: true, actionKey: "capital:archive"),
            ],
            InitialTabIndex = 0,
            Context = new OperationsCenterContext
            {
                EntityType = EntityType.CapitalPartner,
                EntityRow = d,
                SourceModule = AppModule.CapitalPartners,
                Title = d.FullName
            }
        });
    }

    private static UIElement OverviewTab(CapitalPartnerDetailsDto d, CapitalOperationsCenterDto oc)
    {
        var s = new StackPanel();
        s.Children.Add(ErpUxFactory.InfoBanner($"حالة الشريك: {d.StatusDisplay} — مخاطر: {d.RiskLevelDisplay}", "info"));
        s.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("البريد", ReadOnly(d.Email ?? "—")),
            ("العنوان", ReadOnly(d.Address ?? "—")),
            ("العملة", ReadOnly(d.DefaultCurrency)),
            ("تاريخ الإنشاء", ReadOnly(d.CreatedAt.ToString("yyyy/MM/dd"))),
            ("أنشئ بواسطة", ReadOnly(d.CreatedByName ?? "—")))));

        if (oc.ScopeSummaries.Count > 0)
        {
            s.Children.Add(ErpUiFactory.SectionTitle("رأس المال حسب النطاق"));
            var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, Margin = new Thickness(0, 8, 0, 0) };
            ErpUiFactory.AddGridColumn(grid, "النطاق", nameof(CapitalScopeSummaryDto.ScopeDisplay), "*");
            ErpUiFactory.AddGridColumn(grid, "العدد", nameof(CapitalScopeSummaryDto.Count), 80);
            ErpUiFactory.AddGridColumn(grid, "رأس المال", nameof(CapitalScopeSummaryDto.CapitalBase), 120, "N2");
            grid.ItemsSource = oc.ScopeSummaries;
            s.Children.Add(ErpUiFactory.Card(grid));
        }
        return s;
    }

    private static UIElement FinancialTab(CapitalFinancialSummaryDto f) =>
        ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("رأس المال الحالي", ReadOnly($"{f.CurrentCapitalBase:N2} {f.BaseCurrency}")),
            ("إجمالي الاستثمارات", ReadOnly($"{f.TotalInvestmentsBase:N2}")),
            ("إجمالي السحوبات", ReadOnly($"{f.TotalWithdrawalsBase:N2}")),
            ("أرباح موزعة", ReadOnly($"{f.DistributedProfitBase:N2}")),
            ("أرباح غير موزعة", ReadOnly($"{f.UndistributedProfitBase:N2}"))));

    private static UIElement ParticipationsTab(CapitalPartnerDetailsDto d)
    {
        var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true };
        ErpUiFactory.AddGridColumn(grid, "النطاق", nameof(PartnerParticipationDto.ScopeDisplay), 100);
        ErpUiFactory.AddGridColumn(grid, "النسبة %", nameof(PartnerParticipationDto.OwnershipPercentage), 80, "N2");
        ErpUiFactory.AddGridColumn(grid, "المشروع", nameof(PartnerParticipationDto.ProjectCode), 100);
        ErpUiFactory.AddGridColumn(grid, "الحاوية", nameof(PartnerParticipationDto.ContainerNumber), 100);
        ErpUiFactory.AddGridColumn(grid, "نشط", nameof(PartnerParticipationDto.IsActive), 60);
        grid.ItemsSource = d.Participations;
        return ErpUiFactory.Card(grid);
    }

    private static UIElement LedgerTab(CapitalPartnerDetailsDto d)
    {
        var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true };
        ErpUiFactory.AddGridColumn(grid, "التاريخ", nameof(CapitalTransactionDto.TransactionDate), 95, "yyyy/MM/dd");
        ErpUiFactory.AddGridColumn(grid, "النوع", nameof(CapitalTransactionDto.TypeDisplay), 120);
        ErpUiFactory.AddGridColumn(grid, "المبلغ", nameof(CapitalTransactionDto.AmountOriginal), 100, "N2");
        ErpUiFactory.AddGridColumn(grid, "العملة", nameof(CapitalTransactionDto.Currency), 60);
        ErpUiFactory.AddGridColumn(grid, "بالأساس", nameof(CapitalTransactionDto.SignedBaseAmount), 110, "N2");
        ErpUiFactory.AddGridColumn(grid, "النطاق", nameof(CapitalTransactionDto.ScopeDisplay), 80);
        grid.ItemsSource = d.Transactions;
        return ErpUiFactory.Card(grid);
    }

    private static UIElement AuditTab(IReadOnlyList<PartnerAuditEntryDto> audit)
    {
        var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true };
        ErpUiFactory.AddGridColumn(grid, "الوقت", nameof(PartnerAuditEntryDto.Timestamp), 130, "yyyy/MM/dd HH:mm");
        ErpUiFactory.AddGridColumn(grid, "الإجراء", nameof(PartnerAuditEntryDto.Action), 120);
        ErpUiFactory.AddGridColumn(grid, "المستخدم", nameof(PartnerAuditEntryDto.UserName), 100);
        ErpUiFactory.AddGridColumn(grid, "ملاحظات", nameof(PartnerAuditEntryDto.Notes), "*");
        grid.ItemsSource = audit;
        return ErpUiFactory.Card(grid);
    }

    private static UIElement TimelineTab(IReadOnlyList<PartnerTimelineEventDto> timeline)
    {
        var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true };
        ErpUiFactory.AddGridColumn(grid, "الوقت", nameof(PartnerTimelineEventDto.Timestamp), 130, "yyyy/MM/dd HH:mm");
        ErpUiFactory.AddGridColumn(grid, "العنوان", nameof(PartnerTimelineEventDto.Title), 150);
        ErpUiFactory.AddGridColumn(grid, "الوصف", nameof(PartnerTimelineEventDto.Description), "*");
        grid.ItemsSource = timeline;
        return ErpUiFactory.Card(grid);
    }

    private static UIElement NotesTab(CapitalPartnerDetailsDto d) =>
        ErpUiFactory.Card(new TextBlock { Text = d.Notes ?? "لا توجد ملاحظات.", TextWrapping = TextWrapping.Wrap });

    private static UIElement FutureTab() =>
        ErpUxFactory.InfoBanner("جاهز للتكامل المحاسبي — ستُنشأ قيود تلقائية عند اعتماد المعاملات المالية.", "info");

    private static TextBlock ReadOnly(string text) => new() { Text = text, Margin = new Thickness(0, 4, 0, 4) };

    private static OperationsCenterTab Tab(string key, string label, Func<UIElement> contentFactory) =>
        new() { Key = key, Label = label, ContentFactory = contentFactory };

    private static OperationsCenterQuickAction Q(string label, bool primary, string? tab,
        bool destructive = false, bool confirm = false, string? actionKey = null) =>
        new() { Label = label, Primary = primary, TabKey = tab, Destructive = destructive, RequiresConfirmation = confirm, ActionKey = actionKey };

    private static System.Windows.Media.Brush Br(string key) =>
        (System.Windows.Media.Brush)System.Windows.Application.Current.Resources[key]!;

    private static TabControl? FindTabs(DependencyObject root)
    {
        if (root is TabControl tc) return tc;
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            var found = FindTabs(child);
            if (found != null) return found;
        }
        return null;
    }

    private void SelectTab(string tab)
    {
        if (_tabs is null) return;
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Overview"] = 0, ["Financial"] = 1, ["Participations"] = 2, ["Ledger"] = 3,
            ["Audit"] = 4, ["Timeline"] = 5, ["Investment"] = 3, ["Withdrawal"] = 3
        };
        if (map.TryGetValue(tab, out var idx) && idx < _tabs.Items.Count)
            _tabs.SelectedIndex = idx;
    }
}

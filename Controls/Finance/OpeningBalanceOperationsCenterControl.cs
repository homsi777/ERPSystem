using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Documents;
using ERPSystem.Services.Finance;
using ERPSystem.Diagnostics.Performance;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Finance;

public sealed class OpeningBalanceOperationsCenterControl : UserControl
{
    private OpeningBalanceDetailsDto? _loaded;
    private TabControl? _tabs;
    private Guid _documentId;

    public OpeningBalanceOperationsCenterControl()
    {
        Content = new TextBlock { Text = "جاري التحميل...", Margin = new Thickness(24) };
        OpeningBalanceListRefreshHub.RefreshRequested += OnRefreshRequested;
        Unloaded += (_, _) => OpeningBalanceListRefreshHub.RefreshRequested -= OnRefreshRequested;
    }

    private void OnRefreshRequested(object? sender, EventArgs e)
    {
        if (_documentId != Guid.Empty)
            _ = LoadAsync(_documentId);
    }

    public void Initialize(Guid documentId, string? initialTab = null)
    {
        _documentId = documentId;
        Loaded += async (_, _) =>
        {
            await LoadAsync(documentId);
            if (_tabs != null && !string.IsNullOrWhiteSpace(initialTab))
                SelectTab(initialTab);
        };
    }

    private async Task LoadAsync(Guid documentId)
    {
        if (!AppServices.IsInitialized) return;
        using var perfScope = ScreenLoadProfiler.Begin("Finance.OpeningBalanceOperationsCenter");
        var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => OpeningBalanceUiService.Instance.GetDetailsAsync(documentId));
        perfScope?.IncrementServiceCalls();
        if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;
        _loaded = result.Value;
        var shell = BuildShell(_loaded);
        Content = shell;
        _tabs = FindTabs(shell);
    }

    private UserControl BuildShell(OpeningBalanceDetailsDto oc)
    {
        var h = oc.Header;
        return OperationsCenterShell.Build(new OperationsCenterSpec
        {
            Title = h.Number,
            Subtitle = OpeningBalanceDisplay.TypeName(h.Type),
            Breadcrumb = "الأمل.AB › المالية › أرصدة افتتاحية",
            IconGlyph = "\uE8F1",
            Accent = Br("AccentPrimaryBrush"),
            AccentLight = Br("PrimaryVeryLightBrush"),
            StatusBadge = h.StatusDisplay,
            HeaderFields =
            [
                ("النوع", h.TypeDisplay),
                ("التاريخ", h.OpeningDate.ToString("yyyy/MM/dd")),
                ("العملة", h.CurrencyCode),
                ("الإجمالي", $"{h.TotalBaseAmount:N2}"),
                ("القيد", h.JournalEntryNumber ?? "—"),
                ("المصدر", h.SourceDisplay)
            ],
            Kpis =
            [
                ("مدين", $"{h.TotalDebit:N2}", "\uE8C1"),
                ("دائن", $"{h.TotalCredit:N2}", "\uE8C8"),
                ("السطور", h.LineCount.ToString(), "\uE8A5"),
                ("الحالة", h.StatusDisplay, "\uE73E")
            ],
            Workflow =
            [
                ("مسودة", h.Status == OpeningBalanceStatus.Draft, h.Status != OpeningBalanceStatus.Draft),
                ("اعتماد", h.Status is OpeningBalanceStatus.PendingApproval or OpeningBalanceStatus.Approved or OpeningBalanceStatus.Posted or OpeningBalanceStatus.Locked,
                    h.Status is OpeningBalanceStatus.Approved or OpeningBalanceStatus.Posted or OpeningBalanceStatus.Locked),
                ("ترحيل", h.Status is OpeningBalanceStatus.Posted or OpeningBalanceStatus.Locked, h.Status is OpeningBalanceStatus.Posted or OpeningBalanceStatus.Locked),
                ("قفل", h.Status == OpeningBalanceStatus.Locked, h.Status == OpeningBalanceStatus.Locked)
            ],
            Tabs =
            [
                Tab("Overview", "نظرة عامة", () => OverviewTab(oc)),
                Tab("Accounting", "المحاسبة", () => AccountingTab(oc)),
                Tab("Audit", "التدقيق", () => AuditTab(oc.Events)),
                Tab("Timeline", "الخط الزمني", () => TimelineTab(oc.Events)),
                Tab("Journal", "قيود اليومية", () => JournalTab(oc)),
                Tab("Reports", "التقارير", () => ReportsTab(oc))
            ],
            QuickActions = BuildQuickActions(h),
            Context = new OperationsCenterContext
            {
                EntityType = EntityType.OpeningBalance,
                EntityRow = h,
                SourceModule = AppModule.Accounting,
                Title = h.Number
            }
        });
    }

    private static IReadOnlyList<OperationsCenterQuickAction> BuildQuickActions(OpeningBalanceListDto h)
    {
        var actions = new List<OperationsCenterQuickAction>();
        if (h.Status is OpeningBalanceStatus.Draft or OpeningBalanceStatus.Rejected)
            actions.Add(Q("إرسال للاعتماد", true, actionKey: "ob:submit"));
        if (h.Status is OpeningBalanceStatus.PendingApproval or OpeningBalanceStatus.Draft)
            actions.Add(Q("اعتماد", h.Status == OpeningBalanceStatus.PendingApproval, actionKey: "ob:approve"));
        if (h.Status == OpeningBalanceStatus.Approved)
            actions.Add(Q("ترحيل", true, actionKey: "ob:post"));
        if (h.Status != OpeningBalanceStatus.Archived)
            actions.Add(Q("أرشفة", false, destructive: true, actionKey: "ob:archive", requiresConfirmation: true));
        actions.Add(Q("تصدير Excel", false, actionKey: "ob:export"));
        return actions;
    }

    private static UIElement OverviewTab(OpeningBalanceDetailsDto oc) =>
        ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("الوصف", ReadOnly(oc.Header.Description ?? "—")),
            ("المرجع", ReadOnly(oc.Header.Reference ?? "—")),
            ("ملاحظات الاعتماد", ReadOnly(oc.ApprovalNotes ?? "—")),
            ("سبب الرفض", ReadOnly(oc.RejectionReason ?? "—"))));

    private static UIElement AccountingTab(OpeningBalanceDetailsDto oc)
    {
        if (oc.Lines.Count == 0)
            return ErpUxFactory.InfoBanner("لا توجد سطور.", "neutral");
        return ErpUiFactory.Card(ErpUiFactory.BuildGrid(oc.Lines.Select(l => new
        {
            السطر = l.LineNumber,
            الطرف = l.PartyName ?? "—",
            الحساب = l.AccountName ?? "—",
            مستودع = l.WarehouseName ?? "—",
            صنف = l.ItemName ?? "—",
            مدين = l.Debit,
            دائن = l.Credit
        }).ToList(), false));
    }

    private static UIElement JournalTab(OpeningBalanceDetailsDto oc)
    {
        if (oc.JournalLines.Count == 0)
            return ErpUxFactory.InfoBanner("لم يُرحّل بعد أو لا توجد قيود.", "neutral");
        return ErpUiFactory.Card(ErpUiFactory.BuildGrid(oc.JournalLines.Select(j => new
        {
            القيد = j.EntryNumber,
            التاريخ = j.EntryDate.ToString("yyyy/MM/dd"),
            الحساب = $"{j.AccountCode} — {j.AccountName}",
            مدين = j.Debit,
            دائن = j.Credit,
            البيان = j.Narrative ?? "—"
        }).ToList(), false));
    }

    private static UIElement AuditTab(IReadOnlyList<OpeningBalanceEventDto> events) =>
        events.Count == 0
            ? ErpUxFactory.InfoBanner("لا يوجد سجل تدقيق.", "neutral")
            : ErpUiFactory.Card(ErpUiFactory.BuildGrid(events.Select(e => new
            {
                التاريخ = e.OccurredAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm"),
                الإجراء = e.Action,
                المستخدم = e.UserName,
                الجهاز = e.MachineName ?? "—",
                الملاحظات = e.Notes ?? "—"
            }).ToList(), false));

    private static UIElement TimelineTab(IReadOnlyList<OpeningBalanceEventDto> events) => AuditTab(events);

    private static UIElement ReportsTab(OpeningBalanceDetailsDto oc)
    {
        var panel = new StackPanel();
        panel.Children.Add(ErpUxFactory.InfoBanner($"تقرير {OpeningBalanceDisplay.TypeName(oc.Header.Type)} — {oc.Header.Number}", "info"));
        var exportBtn = new Button { Content = "تصدير Excel", Margin = new Thickness(0, 8, 0, 0), Padding = new Thickness(12, 6, 12, 6) };
        exportBtn.Click += (_, _) => ListExportService.ExportRecords(
            oc.Lines,
            $"OpeningBalance-{oc.Header.Number}",
            ("السطر", l => l.LineNumber),
            ("الطرف", l => l.PartyName),
            ("مدين", l => l.Debit),
            ("دائن", l => l.Credit),
            ("الوصف", l => l.Description));
        panel.Children.Add(exportBtn);
        return panel;
    }

    private static TextBox ReadOnly(string text) => new()
    {
        Text = text,
        IsReadOnly = true,
        BorderThickness = new Thickness(0),
        Background = System.Windows.Media.Brushes.Transparent
    };

    private void SelectTab(string key)
    {
        if (_tabs is null) return;
        for (var i = 0; i < _tabs.Items.Count; i++)
        {
            if (_tabs.Items[i] is TabItem ti && ti.Tag?.ToString()?.Equals(key, StringComparison.OrdinalIgnoreCase) == true)
            {
                _tabs.SelectedIndex = i;
                return;
            }
        }
    }

    private static TabControl? FindTabs(DependencyObject root)
    {
        if (root is TabControl tc) return tc;
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var found = FindTabs(System.Windows.Media.VisualTreeHelper.GetChild(root, i));
            if (found is not null) return found;
        }

        return null;
    }

    private static OperationsCenterTab Tab(string key, string label, Func<UIElement> contentFactory) =>
        new() { Key = key, Label = label, ContentFactory = contentFactory };

    private static OperationsCenterQuickAction Q(string label, bool primary, string? tab = null,
        bool destructive = false, string? actionKey = null, bool requiresConfirmation = false) =>
        new()
        {
            Label = label,
            Primary = primary,
            TabKey = tab,
            Destructive = destructive,
            ActionKey = actionKey,
            RequiresConfirmation = requiresConfirmation
        };

    private static System.Windows.Media.SolidColorBrush Br(string k) =>
        (System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
}

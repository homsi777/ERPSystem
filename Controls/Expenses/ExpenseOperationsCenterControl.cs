using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Expenses;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Expenses;

public sealed class ExpenseOperationsCenterControl : UserControl
{
    private ExpenseOperationsCenterDto? _loaded;
    private TabControl? _tabs;

    public ExpenseOperationsCenterControl()
    {
        Content = new TextBlock { Text = "جاري التحميل...", Margin = new Thickness(24) };
    }

    public void Initialize(Guid expenseId, string? initialTab = null)
    {
        Loaded += async (_, _) =>
        {
            await LoadAsync(expenseId);
            if (_tabs != null && !string.IsNullOrWhiteSpace(initialTab))
                SelectTab(initialTab);
        };
    }

    private async Task LoadAsync(Guid expenseId)
    {
        if (!AppServices.IsInitialized)
            return;

        var result = await ExpenseUiService.Instance.GetOperationsCenterAsync(expenseId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        _loaded = result.Value;
        var shell = BuildShell(_loaded);
        Content = shell;
        _tabs = FindTabs(shell);
    }

    private UserControl BuildShell(ExpenseOperationsCenterDto oc)
    {
        var d = oc.Details;
        var f = oc.Financial;
        return OperationsCenterShell.Build(new OperationsCenterSpec
        {
            Title = d.Name,
            Subtitle = $"مركز عمل المصروف — {d.Code}",
            Breadcrumb = "ERP PRO › المصاريف › مركز العمل",
            IconGlyph = "\uE9D9",
            Accent = Br("AccentPayableBrush"),
            AccentLight = Br("WarningBgBrush"),
            StatusBadge = d.StatusDisplay,
            HeaderFields =
            [
                ("الكود", d.Code),
                ("الفئة", d.CategoryName),
                ("المبلغ", $"{d.OriginalAmount:N2} {d.OriginalCurrency}"),
                ("المدفوع", $"{f.PaidAmountBase:N2} {f.BaseCurrency}"),
                ("المتبقي", $"{f.RemainingBalanceBase:N2} {f.BaseCurrency}"),
                ("المستفيد", d.PayeeName ?? "—"),
            ],
            Kpis =
            [
                ("الأصلي", $"{d.OriginalAmount:N2}", "\uE8C1"),
                ("المدفوع", $"{f.PaidAmountBase:N2}", "\uE719"),
                ("المتبقي", $"{f.RemainingBalanceBase:N2}", "\uE8AB"),
                ("الدفعات", f.CompletedPayments.ToString(), "\uE8A5"),
                ("مرفقات", d.Attachments.Count.ToString(), "\uE8B7"),
                ("الاستحقاق", f.NextPaymentDue?.ToString("yyyy/MM/dd") ?? "—", "\uE787"),
            ],
            Workflow = oc.LifecycleSteps.Select(s => (s.Label, s.Current, s.Completed)).ToList(),
            Tabs =
            [
                Tab("Overview", "نظرة عامة", OverviewTab(d, oc.Statistics)),
                Tab("Financial", "الملخص المالي", FinancialTab(f, d)),
                Tab("Lifecycle", "دورة الحياة", LifecycleTab(d)),
                Tab("Payments", "سجل الدفعات", PaymentsTab(d)),
                Tab("Installments", "الأقساط", InstallmentsTab(d)),
                Tab("Attachments", "المرفقات", AttachmentsTab(d)),
                Tab("Audit", "سجل التدقيق", AuditTab(oc.RecentAudit)),
                Tab("Timeline", "الخط الزمني", TimelineTab(oc.Timeline)),
                Tab("Notes", "ملاحظات", NotesTab(d)),
                Tab("Related", "ارتباطات", RelatedTab(d)),
                Tab("FutureAccounting", "المحاسبة المستقبلية", FutureIntegrationTab("المحاسبة", "سيتم ربط قيود المصروف تلقائياً عند تفعيل وحدة المحاسبة.")),
                Tab("FutureTreasury", "الخزينة المستقبلية", FutureIntegrationTab("الخزينة", "سيتم ربط مصادر التمويل والمدفوعات بوحدة الخزينة.")),
                Tab("Statistics", "إحصائيات", StatisticsTab(oc.Statistics)),
            ],
            QuickActions =
            [
                Q("تعديل", false, null, actionKey: "nav:Expenses:Form"),
                Q("اعتماد", false, null, actionKey: "expense:approve"),
                Q("تسجيل دفعة", false, null, actionKey: "expense:record-payment"),
                Q("نسخ", false, null, actionKey: "expense:duplicate"),
                Q("إغلاق", false, null, actionKey: "expense:close"),
                Q("أرشفة", false, null, destructive: true, confirm: true, actionKey: "expense:archive"),
                Q("PDF", false, null, actionKey: "preview:تقرير مصروف"),
            ],
            InitialTabIndex = 0,
            Context = new OperationsCenterContext
            {
                EntityType = Core.Actions.EntityType.Expense,
                EntityRow = d,
                SourceModule = AppModule.Expenses,
                Title = d.Name
            }
        });
    }

    private static UIElement OverviewTab(ExpenseDetailsDto d, ExpenseStatisticsDto stats)
    {
        var s = new StackPanel();
        s.Children.Add(ErpUxFactory.InfoBanner(
            d.IsRecurring && d.NextDueDate is not null
                ? $"مصروف متكرر — الاستحقاق التالي: {d.NextDueDate:yyyy/MM/dd}"
                : $"حالة المصروف: {d.StatusDisplay}", d.IsRecurring ? "info" : "neutral"));
        s.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("الوصف", ReadOnly(d.Description ?? "—")),
            ("مركز التكلفة", ReadOnly(d.CostCenterName ?? "—")),
            ("القسم", ReadOnly(d.Department ?? "—")),
            ("المشروع", ReadOnly(d.ProjectCode ?? "—")),
            ("طريقة الدفع", ReadOnly(d.PaymentMethodDisplay)),
            ("تاريخ البداية", ReadOnly(d.StartDate.ToString("yyyy/MM/dd"))),
            ("تاريخ النهاية", ReadOnly(d.EndDate?.ToString("yyyy/MM/dd") ?? "—")),
            ("عمر السجل", ReadOnly($"{stats.DaysSinceCreated} يوم")))));
        return s;
    }

    private static UIElement FinancialTab(ExpenseFinancialSummaryDto f, ExpenseDetailsDto d) =>
        ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("العملة الأصلية", ReadOnly($"{f.OriginalAmount:N2} {f.OriginalCurrency}")),
            ("سعر الصرف", ReadOnly(d.ExchangeRate.ToString("N4"))),
            ("بالعملة الأساس", ReadOnly($"{f.BaseAmount:N2} {f.BaseCurrency}")),
            ("المدفوع", ReadOnly($"{f.PaidAmountBase:N2} {f.BaseCurrency}")),
            ("المتبقي", ReadOnly($"{f.RemainingBalanceBase:N2} {f.BaseCurrency}")),
            ("دفعات مكتملة", ReadOnly(f.CompletedPayments.ToString())),
            ("دفعات مجدولة", ReadOnly(f.ScheduledPayments.ToString())),
            ("أقساط معلقة", ReadOnly(f.PendingInstallments.ToString()))));

    private static UIElement LifecycleTab(ExpenseDetailsDto d)
    {
        var panel = new StackPanel();
        panel.Children.Add(ErpUxFactory.InfoBanner($"الحالة الحالية: {d.StatusDisplay}", "info"));
        if (d.AllowedTransitions.Count > 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "الانتقالات المسموحة: " + string.Join("، ", d.AllowedTransitions.Select(s => s.ToString())),
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        }
        return panel;
    }

    private static UIElement PaymentsTab(ExpenseDetailsDto d)
    {
        if (d.Payments.Count == 0)
            return ErpUxFactory.InfoBanner("لا توجد دفعات مسجّلة بعد.", "neutral");

        return ErpUiFactory.Card(ErpUiFactory.BuildGrid(d.Payments.Select(p => new
        {
            التاريخ = p.PaymentDate.ToString("yyyy/MM/dd"),
            الاستحقاق = p.DueDate?.ToString("yyyy/MM/dd") ?? "—",
            المبلغ = p.AmountOriginal.ToString("N2"),
            العملة = p.Currency,
            بالأساس = p.AmountBase.ToString("N2"),
            الطريقة = p.PaymentMethodDisplay,
            المصدر = p.FundingSourceDisplay,
            الحالة = p.StatusDisplay,
            المرجع = p.ReferenceNumber ?? "—"
        }).ToList(), false));
    }

    private static UIElement InstallmentsTab(ExpenseDetailsDto d)
    {
        if (d.Installments.Count == 0)
            return ErpUxFactory.InfoBanner("لا توجد أقساط مجدولة.", "neutral");

        return ErpUiFactory.Card(ErpUiFactory.BuildGrid(d.Installments.Select(i => new
        {
            القسط = i.InstallmentNumber,
            الاستحقاق = i.DueDate.ToString("yyyy/MM/dd"),
            المبلغ = i.AmountOriginal.ToString("N2"),
            بالأساس = i.AmountBase.ToString("N2"),
            الحالة = i.StatusDisplay
        }).ToList(), false));
    }

    private static UIElement AttachmentsTab(ExpenseDetailsDto d)
    {
        if (d.Attachments.Count == 0)
            return PlaceholderUi.DatabasePhase("لم يُرفع مرفق بعد");

        return ErpUiFactory.Card(ErpUiFactory.BuildGrid(d.Attachments.Select(a => new
        {
            الملف = a.FileName,
            النوع = a.ContentType,
            الحجم = $"{a.SizeBytes / 1024.0:N1} KB"
        }).ToList(), false));
    }

    private static UIElement AuditTab(IReadOnlyList<ExpenseAuditEntryDto> audit)
    {
        if (audit.Count == 0)
            return ErpUxFactory.InfoBanner("لا يوجد سجل تدقيق بعد.", "neutral");

        return ErpUiFactory.Card(ErpUiFactory.BuildGrid(audit.Select(a => new
        {
            التاريخ = a.Timestamp.ToLocalTime().ToString("yyyy/MM/dd HH:mm"),
            الإجراء = a.Action,
            الحقل = a.FieldName ?? "—",
            السابق = a.PreviousValue ?? "—",
            الجديد = a.NewValue ?? "—",
            المستخدم = a.UserName,
            السبب = a.Reason ?? "—"
        }).ToList(), false));
    }

    private static UIElement TimelineTab(IReadOnlyList<ExpenseTimelineEventDto> events)
    {
        if (events.Count == 0)
            return ErpUxFactory.InfoBanner("لا توجد أحداث في الخط الزمني بعد.", "neutral");

        return ErpUiFactory.Card(ErpUiFactory.BuildGrid(events.Select(e => new
        {
            التاريخ = e.Timestamp.ToLocalTime().ToString("yyyy/MM/dd HH:mm"),
            الحدث = e.Title,
            النوع = e.EventType,
            السابق = e.PreviousValue ?? "—",
            الجديد = e.NewValue ?? "—",
            المستخدم = e.UserName,
            السبب = e.Reason ?? "—"
        }).ToList(), false));
    }

    private static UIElement NotesTab(ExpenseDetailsDto d) =>
        ErpUiFactory.Card(new TextBox
        {
            Text = d.Notes ?? "",
            IsReadOnly = true,
            Height = 120,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true
        });

    private static UIElement RelatedTab(ExpenseDetailsDto d) =>
        ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("مركز التكلفة", ReadOnly(d.CostCenterName ?? "—")),
            ("القسم", ReadOnly(d.Department ?? "—")),
            ("المشروع", ReadOnly(d.ProjectCode ?? "—")),
            ("المورد", ReadOnly(d.PayeeName ?? "—")),
            ("التكرار", ReadOnly(d.IsRecurring ? d.RecurrenceDisplay : "غير متكرر"))));

    private static UIElement FutureIntegrationTab(string title, string message) =>
        ErpUiFactory.Card(new StackPanel
        {
            Children =
            {
                ErpUiFactory.SectionTitle(title),
                ErpUxFactory.InfoBanner(message, "info")
            }
        });

    private static UIElement StatisticsTab(ExpenseStatisticsDto stats) =>
        ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("إجمالي الدفعات", ReadOnly(stats.TotalPayments.ToString())),
            ("المرفقات", ReadOnly(stats.TotalAttachments.ToString())),
            ("أيام منذ الإنشاء", ReadOnly(stats.DaysSinceCreated.ToString())),
            ("أحداث التدقيق", ReadOnly(stats.AuditEventCount.ToString()))));

    private static TextBox ReadOnly(string text) => new()
    {
        Text = text,
        IsReadOnly = true,
        BorderThickness = new Thickness(0),
        Background = System.Windows.Media.Brushes.Transparent
    };

    private void SelectTab(string key)
    {
        if (_tabs == null) return;
        for (int i = 0; i < _tabs.Items.Count; i++)
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
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            var found = FindTabs(child);
            if (found != null) return found;
        }
        return null;
    }

    private static OperationsCenterTab Tab(string key, string label, UIElement content) =>
        new() { Key = key, Label = label, Content = content };

    private static OperationsCenterQuickAction Q(string label, bool primary, string? tab,
        bool destructive = false, bool confirm = false, string? actionKey = null) =>
        new() { Label = label, Primary = primary, TabKey = tab, Destructive = destructive, RequiresConfirmation = confirm, ActionKey = actionKey };

    private static System.Windows.Media.SolidColorBrush Br(string k) =>
        (System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
}

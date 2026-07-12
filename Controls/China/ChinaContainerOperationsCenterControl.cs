using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.ChinaImport;
using ERPSystem.Domain.Entities.System;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.China;
using ERPSystem.Diagnostics.Performance;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.China;

public sealed class ChinaContainerOperationsCenterControl : UserControl
{
    private readonly TextBlock _loading = new()
    {
        Text = "جاري تحميل مركز عمليات الحاوية...",
        Margin = new Thickness(24),
        FontSize = 14,
        Foreground = Brushes.Gray
    };

    private Guid _containerId;
    private string _initialTab = "Overview";

    public ChinaContainerOperationsCenterControl()
    {
        Content = _loading;
        ErpDataRefreshHub.DataChanged += OnDataRefreshRequested;
        Unloaded += (_, _) => ErpDataRefreshHub.DataChanged -= OnDataRefreshRequested;
    }

    private void OnDataRefreshRequested(ErpDataRefreshScope scope)
    {
        if (_containerId == Guid.Empty)
            return;
        if ((scope & (ErpDataRefreshScope.OperationsCenter | ErpDataRefreshScope.All)) == 0)
            return;

        if (!IsLoaded)
            return;

        _ = ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (!AppServices.IsInitialized)
            return;

        var opsResult = await ContainerUiService.Instance.GetOperationsCenterAsync(_containerId);
        if (!opsResult.IsSuccess || opsResult.Value is null)
            return;

        var audit = await ContainerUiService.Instance.GetAuditTrailAsync(_containerId);
        var row = ContainerListRow.FromDetails(opsResult.Value.Container);
        Content = BuildShell(opsResult.Value, row, audit, _initialTab);
    }

    public void Initialize(Guid containerId, string initialTab)
    {
        _containerId = containerId;
        _initialTab = initialTab;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (!AppServices.IsInitialized)
            return;

        using var perfScope = ScreenLoadProfiler.Begin("China.OperationsCenter");
        var opsResult = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => ContainerUiService.Instance.GetOperationsCenterAsync(_containerId));
        perfScope?.IncrementServiceCalls();
        if (!ApplicationResultPresenter.Present(opsResult) || opsResult.Value is null)
        {
            Content = new TextBlock { Text = "تعذّر تحميل بيانات الحاوية.", Margin = new Thickness(24) };
            return;
        }

        var audit = await ContainerUiService.Instance.GetAuditTrailAsync(_containerId);
        var row = ContainerListRow.FromDetails(opsResult.Value.Container);
        ChinaImportNavigationContext.SetActiveContainer(_containerId);
        Content = BuildShell(opsResult.Value, row, audit, _initialTab);
    }

    private static UserControl BuildShell(
        ContainerOperationsCenterDto data,
        ContainerListRow row,
        IReadOnlyList<AuditLog> audit,
        string initialTab)
    {
        var c = data.Container;
        var accent = new SolidColorBrush(Color.FromRgb(124, 58, 237));
        var cost = c.LandingCost;
        var inv = data.Inventory;
        var reservedRolls = inv?.ReservedRolls ?? 0;
        var soldRolls = inv?.SoldRolls ?? 0;
        var remainingRolls = inv?.AvailableRolls ?? Math.Max(0, c.TotalRolls - reservedRolls - soldRolls);
        var reservedMeters = inv?.ReservedMeters ?? 0m;
        var soldMeters = inv?.SoldMeters ?? 0m;
        var availableMeters = inv?.AvailableMeters ?? c.TotalMeters;
        var costPerMeter = inv?.CostPerMeter ?? (cost is null ? 0m : cost.CustomsCostPerMeter + cost.ExpenseCostPerMeter);

        return OperationsCenterShell.Build(new OperationsCenterSpec
        {
            Title = c.ContainerNumber,
            Subtitle = "مركز عمليات الحاوية — بيانات PostgreSQL",
            Breadcrumb = "الأمل.AB › طلبات الصين › مركز العمليات",
            IconGlyph = "\uE7BF",
            Accent = accent,
            AccentLight = Br("PrimaryVeryLightBrush"),
            StatusBadge = row.StatusDisplay,
            HeaderFields =
            [
                ("المورد", row.SupplierName),
                ("تاريخ الشحن", c.ShipmentDate.ToString("yyyy/MM/dd")),
                ("الوصول", c.ArrivalDate?.ToString("yyyy/MM/dd") ?? "—"),
                ("الأثواب", AppFormats.Number(c.TotalRolls)),
                ("الأطوال", $"{c.TotalMeters:N0} م"),
                ("الوزن", c.TotalWeightKg.HasValue ? $"{c.TotalWeightKg:N0} كغ" : "—"),
                ("سعر الصرف", AppFormats.Number(c.ExchangeRateToLocalCurrency, 4)),
            ],
            Kpis =
            [
                ("أنواع الأقمشة", row.CodeCount.ToString(), "\uECA5"),
                ("الألوان", row.ColorCount.ToString(), "\uE790"),
                ("الأثواب", AppFormats.Number(c.TotalRolls), "\uE7C3"),
                ("الأطوال", $"{c.TotalMeters:N0} م", "\uE821"),
                ("محجوز", $"{reservedRolls:N0} ({reservedMeters:N0} م)", "\uE823"),
                ("مباع", $"{soldRolls:N0} ({soldMeters:N0} م)", "\uE8F1"),
                ("متبقي", $"{remainingRolls:N0} ({availableMeters:N0} م)", "\uE8FD"),
                ("تكلفة/م", costPerMeter > 0 ? $"{costPerMeter:N4}" : "—", "\uE8C1"),
            ],
            Workflow = ChinaImportWorkflow.BuildStepper(c.Status)
                .Select(s => (s.Label, s.Completed, s.Current))
                .ToList(),
            Tabs =
            [
                Tab("Overview", "نظرة عامة", () => OverviewTab(c, cost)),
                Tab("Items", "أصناف الحاوية", () => ItemsTab(c)),
                Tab("LandingCost", "Landing Cost", () => LandingCostTab(cost, data)),
                Tab("Documentation", "التوثيق", () => new ContainerDocumentationControl(c.Id, c.ContainerNumber)),
                Tab("Timeline", "الخط الزمني", () => TimelineTab(audit)),
            ],
            QuickActions = BuildQuickActions(data, row),
            InitialTabIndex = ResolveTabIndex(initialTab, "Overview", "Items", "LandingCost", "Documentation", "Timeline"),
            Context = new OperationsCenterContext
            {
                EntityType = EntityType.ImportContainer,
                EntityRow = row,
                SourceModule = AppModule.ChinaImport,
                Title = c.ContainerNumber
            }
        });
    }

    private static IReadOnlyList<OperationsCenterQuickAction> BuildQuickActions(
        ContainerOperationsCenterDto data,
        ContainerListRow row)
    {
        var actions = new List<OperationsCenterQuickAction>
        {
            Q("مراجعة الاستيراد", true, "Items"),
            Q("استيراد Excel", false, null, actionKey: "nav:ChinaImport:NewImport"),
            Q("Landing Cost", false, "LandingCost", actionKey: "ws:LandingCost"),
            Q("ملفات التوثيق", false, "Documentation", actionKey: "china:Documentation"),
            Q("طباعة ملخص", false, null, actionKey: "preview:ملخص الحاوية"),
        };

        if (data.CanApprove)
            actions.Add(Q("اعتماد الحاوية", false, null, actionKey: "china:Approve"));

        if (data.CanSetSalePrices)
            actions.Add(Q("أسعار البيع", false, null, actionKey: "china:SalePrice"));

        if (data.CanMoveToWarehouse)
            actions.Add(Q("تحويل للمخزن", false, null, actionKey: "china:MoveToWarehouse"));

        if (data.LinkedPurchaseInvoiceId is Guid)
            actions.Add(Q("فاتورة الشراء", false, null, actionKey: "china:PurchaseInvoice"));

        if (row.Status is not ChinaContainerStatus.Archived and not ChinaContainerStatus.Cancelled)
            actions.Add(Q("أرشفة", false, null, destructive: true, confirm: true, actionKey: "china:Archive"));

        return actions;
    }

    private static UIElement OverviewTab(ContainerDetailsDto c, LandingCostDto? cost)
    {
        var stack = new StackPanel();
        if (cost is not null)
        {
            stack.Children.Add(ErpUxFactory.KpiStrip(
                ("تكلفة الجمارك/م", $"{cost.CustomsCostPerMeter:N4}"),
                ("تكلفة المصاريف/م", $"{cost.ExpenseCostPerMeter:N4}"),
                ("غرام/م", $"{cost.AvgGramPerMeter:N2}"),
                ("إجمالي المصاريف", $"{cost.TotalImportExpenses:N0}")));
        }

        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("الحالة", ErpUiFactory.FormField(c.Status.ToArabic())),
            ("إجمالي الأثواب", ErpUiFactory.FormField(AppFormats.Number(c.TotalRolls))),
            ("إجمالي الأطوال", ErpUiFactory.FormField($"{c.TotalMeters:N0} م")),
            ("بنود الحاوية", ErpUiFactory.FormField(c.Items.Count.ToString())),
            ("Landing Cost", ErpUiFactory.FormField(cost is null ? "غير محسوب" : "محسوب")))));
        return stack;
    }

    private static UIElement ItemsTab(ContainerDetailsDto c)
    {
        if (c.Items.Count == 0)
            return ErpUxFactory.InfoBanner("لا توجد بنود مسجّلة لهذه الحاوية.", "info");

        var rows = c.Items.Select(i => new
        {
            السطر = i.LineNumber,
            الأثواب = i.RollCount,
            الأمتار = $"{i.LengthMeters:N2}",
            الحالة = i.IsValid ? "صالح" : "خطأ"
        }).Cast<object>().ToArray();

        return ErpUiFactory.Card(ErpUiFactory.BuildGrid(rows));
    }

    private static UIElement LandingCostTab(LandingCostDto? cost, ContainerOperationsCenterDto? ops = null)
    {
        if (cost is null)
            return ErpUxFactory.InfoBanner("لم تُحسب تكاليف الوصول بعد.", "warning");

        var stack = new StackPanel();
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("إجمالي الطول", ErpUiFactory.FormField($"{cost.TotalLengthMeters:N0} م")),
            ("وزن الحاوية", ErpUiFactory.FormField($"{cost.ContainerWeightKg:N0} كغ")),
            ("الجمارك", ErpUiFactory.FormField($"{cost.CustomsAmount:N0}")),
            ("تكلفة الجمارك/م", ErpUiFactory.FormField($"{cost.CustomsCostPerMeter:N4}")),
            ("الشحن", ErpUiFactory.FormField($"{cost.Shipping:N0}")),
            ("التخليص", ErpUiFactory.FormField($"{cost.Clearance:N0}")),
            ("مصاريف أخرى", ErpUiFactory.FormField($"{cost.OtherExpenses:N0}")),
            ("إجمالي المصاريف", ErpUiFactory.FormField($"{cost.TotalImportExpenses:N0}")),
            ("تكلفة المصاريف/م", ErpUiFactory.FormField($"{cost.ExpenseCostPerMeter:N4}")),
            ("متوسط غرام/م", ErpUiFactory.FormField($"{cost.AvgGramPerMeter:N2}")))));

        if (ops is { CanApprove: true } or { CanSetSalePrices: true })
        {
            stack.Children.Add(ErpUxFactory.InfoBanner(
                ops.CanApprove
                    ? "الحاوية جاهزة للاعتماد. استخدم الإجراء السريع «اعتماد الحاوية» أو افتح شاشة الاعتماد الكاملة من القائمة."
                    : "أدخل أسعار البيع لكل نوع قماش قبل الاعتماد.",
                ops.CanApprove ? "success" : "warning"));
        }

        return stack;
    }

    private static UIElement TimelineTab(IReadOnlyList<AuditLog> audit)
    {
        if (audit.Count == 0)
            return ErpUxFactory.InfoBanner("لا يوجد سجل تدقيق لهذه الحاوية بعد.", "info");

        var rows = audit
            .OrderByDescending(a => a.OccurredAt)
            .Select(a => new
            {
                التاريخ = a.OccurredAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm"),
                الإجراء = a.Action,
                السابق = a.OldValuesJson ?? "—",
                الجديد = a.NewValuesJson ?? "—"
            })
            .Cast<object>()
            .ToArray();

        return ErpUiFactory.Card(ErpUiFactory.BuildGrid(rows));
    }

    private static int ResolveTabIndex(string selected, params string[] keys)
    {
        for (var i = 0; i < keys.Length; i++)
            if (keys[i].Equals(selected, StringComparison.OrdinalIgnoreCase))
                return i;
        return 0;
    }

    private static OperationsCenterTab Tab(string key, string label, Func<UIElement> contentFactory) =>
        new() { Key = key, Label = label, ContentFactory = contentFactory };

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

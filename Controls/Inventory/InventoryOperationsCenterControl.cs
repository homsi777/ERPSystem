using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryOperationsCenterControl : UserControl
{
    public InventoryOperationsCenterControl()
    {
        Content = new TextBlock { Text = "جاري التحميل...", Margin = new Thickness(24) };
        Loaded += async (_, _) =>
        {
            var (id, tab) = InventoryNavigationContext.TakeWorkspaceContext();
            if (!id.HasValue) return;
            await LoadAsync(id.Value, tab);
        };
    }

    private async Task LoadAsync(Guid warehouseId, string? initialTab = null)
    {
        if (!AppServices.IsInitialized) return;
        var result = await InventoryUiService.Instance.GetOperationsCenterAsync(warehouseId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;
        Content = BuildShell(result.Value, initialTab);
    }

    private static UserControl BuildShell(InventoryOperationsCenterDto oc, string? initialTab = null)
    {
        var w = oc.Warehouse;
        return OperationsCenterShell.Build(new OperationsCenterSpec
        {
            Title = w.NameAr,
            Subtitle = $"مركز عمل المستودع — {w.Code}",
            Breadcrumb = "ERP PRO › المخزون › مركز العمل",
            IconGlyph = "\uE8B7",
            Accent = Br("AccentInventoryBrush"),
            AccentLight = Br("SuccessBgBrush"),
            StatusBadge = w.IsActive ? "نشط" : "معطل",
            HeaderFields =
            [
                ("الكود", w.Code),
                ("المدينة", w.City),
                ("المدير", w.Manager ?? "—"),
                ("Rolls", w.RollCount.ToString()),
                ("الأمتار", $"{w.TotalMeters:N1}"),
                ("قيمة المخزون", $"${w.InventoryValue:N2}"),
            ],
            Kpis =
            [
                ("القيمة", $"${oc.InventoryValue:N0}", "\uE8C1"),
                ("Rolls", w.RollCount.ToString(), "\uE8B7"),
                ("الأمتار", $"{w.TotalMeters:N0}", "\uE8CB"),
                ("المواقع", oc.Locations.Count.ToString(), "\uE8F1"),
                ("مناقلات", oc.PendingTransfers.ToString(), "\uE8AB"),
                ("تنبيهات", oc.Alerts.Count.ToString(), "\uE783"),
            ],
            Tabs =
            [
                Tab("Overview", "نظرة عامة", OverviewTab(oc)),
                Tab("Stock", "الأرصدة", StockTab(oc)),
                Tab("Rolls", "Rolls", RollsTab(oc)),
                Tab("Locations", "المواقع", LocationsTab(oc)),
                Tab("Movements", "الحركات", MovementsTab(oc)),
                Tab("Alerts", "التنبيهات", AlertsTab(oc)),
                Tab("Audit", "التدقيق", AuditTab(oc)),
                Tab("Timeline", "الخط الزمني", TimelineTab(oc)),
                Tab("FutureGl", "المحاسبة", FutureTab()),
            ],
            QuickActions =
            [
                Q("تعديل", false, null, actionKey: "form:EditWarehouse"),
                Q("مناقلة", false, null, actionKey: "nav:Inventory:Transfers"),
                Q("جرد", false, null, actionKey: "nav:Inventory:Stocktake"),
            ],
            Context = new OperationsCenterContext
            {
                EntityType = Core.Actions.EntityType.Warehouse,
                EntityRow = w,
                SourceModule = AppModule.Inventory,
                Title = w.NameAr
            },
            InitialTabIndex = ResolveTabIndex(initialTab, "Overview", "Stock", "Rolls", "Locations", "Movements", "Alerts", "Audit", "Timeline", "FutureGl")
        });
    }

    private static int ResolveTabIndex(string? key, params string[] keys)
    {
        if (string.IsNullOrWhiteSpace(key)) return 0;
        for (int i = 0; i < keys.Length; i++)
            if (keys[i].Equals(key, StringComparison.OrdinalIgnoreCase))
                return i;
        return 0;
    }

    private static UIElement OverviewTab(InventoryOperationsCenterDto oc)
    {
        var s = new StackPanel();
        s.Children.Add(ErpUxFactory.InfoBanner($"قيمة المخزون: ${oc.InventoryValue:N2} — {oc.Stock.Count} صنف"));
        s.Children.Add(ErpUiFactory.Card(new TextBlock
        {
            Text = $"• {oc.Rolls.Count} roll\n• {oc.Locations.Count} موقع\n• {oc.RecentMovements.Count} حركة\n• {oc.PendingTransfers} مناقلة\n• {oc.PendingStocktakes} جرد",
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(12)
        }));
        return s;
    }

    private static UIElement StockTab(InventoryOperationsCenterDto oc) => BuildGrid(oc.Stock,
        ("القماش", nameof(FabricStockBalanceDto.FabricName)),
        ("اللون", nameof(FabricStockBalanceDto.ColorName)),
        ("Rolls", nameof(FabricStockBalanceDto.RollCount)),
        ("المتاح", nameof(FabricStockBalanceDto.AvailableMeters)),
        ("القيمة", nameof(FabricStockBalanceDto.InventoryValue)));

    private static UIElement RollsTab(InventoryOperationsCenterDto oc) => BuildGrid(oc.Rolls,
        ("#", nameof(FabricRollListDto.RollNumber)),
        ("القماش", nameof(FabricRollListDto.FabricName)),
        ("المتبقي", nameof(FabricRollListDto.RemainingLengthMeters)),
        ("القيمة", nameof(FabricRollListDto.CurrentValue)),
        ("الحالة", nameof(FabricRollListDto.Status)));

    private static UIElement LocationsTab(InventoryOperationsCenterDto oc) => BuildGrid(oc.Locations,
        ("الكود", nameof(StorageLocationDto.Code)),
        ("الاسم", nameof(StorageLocationDto.Name)),
        ("النوع", nameof(StorageLocationDto.LocationType)),
        ("الحالة", nameof(StorageLocationDto.Status)));

    private static UIElement MovementsTab(InventoryOperationsCenterDto oc) => BuildGrid(oc.RecentMovements,
        ("الرقم", nameof(StockMovementListDto.MovementNumber)),
        ("النوع", nameof(StockMovementListDto.Type)),
        ("الأمتار", nameof(StockMovementListDto.TotalMeters)),
        ("القيمة", nameof(StockMovementListDto.TotalValue)));

    private static UIElement AlertsTab(InventoryOperationsCenterDto oc) => BuildGrid(oc.Alerts,
        ("النوع", nameof(InventoryAlertDto.AlertType)),
        ("العنوان", nameof(InventoryAlertDto.Title)),
        ("الرسالة", nameof(InventoryAlertDto.Message)));

    private static UIElement AuditTab(InventoryOperationsCenterDto oc) => BuildGrid(oc.RecentAudit,
        ("التاريخ", nameof(InventoryAuditDto.RecordedAt)),
        ("الإجراء", nameof(InventoryAuditDto.Action)),
        ("المستخدم", nameof(InventoryAuditDto.Username)));

    private static UIElement TimelineTab(InventoryOperationsCenterDto oc) => BuildGrid(oc.Timeline,
        ("التاريخ", nameof(InventoryTimelineDto.OccurredAt)),
        ("الحدث", nameof(InventoryTimelineDto.Title)),
        ("المستخدم", nameof(InventoryTimelineDto.Username)));

    private static UIElement FutureTab() =>
        ErpUxFactory.InfoBanner("سيتم ربط قيود المخزون تلقائياً بوحدة المحاسبة عند التفعيل.");

    private static OperationsCenterTab Tab(string key, string label, UIElement content) =>
        new() { Key = key, Label = label, Content = new ScrollViewer { Content = content, Padding = new Thickness(16) } };

    private static OperationsCenterQuickAction Q(string label, bool primary, string? tab,
        bool destructive = false, bool confirm = false, string? actionKey = null) =>
        new() { Label = label, Primary = primary, TabKey = tab, Destructive = destructive, RequiresConfirmation = confirm, ActionKey = actionKey };

    private static DataGrid BuildGrid<T>(IReadOnlyList<T> data, params (string Header, string Path)[] cols)
    {
        var g = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, ItemsSource = data };
        foreach (var (h, p) in cols)
            ErpUiFactory.AddGridColumn(g, h, p, "*", null);
        return g;
    }

    private static System.Windows.Media.Brush Br(string k) =>
        (System.Windows.Media.Brush)System.Windows.Application.Current.Resources[k]!;
}

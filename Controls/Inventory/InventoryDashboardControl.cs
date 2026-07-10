using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryDashboardControl : UserControl
{
    private readonly ComboBox _warehouseFilter = InventoryContainerFilterUi.CreateComboBox();
    private readonly ComboBox _containerFilter = InventoryContainerFilterUi.CreateComboBox();
    private readonly TextBox _searchBox = InventoryContainerFilterUi.CreateSearchBox(360);
    private readonly Border _insightHost = InventoryContainerFilterUi.CreateInsightHost();
    private readonly StackPanel _contentPanel = new();
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(280) };
    private bool _suppressFilterChange;
    private bool _shellBuilt;
    private IReadOnlyList<FabricStockBalanceDto> _allStock = [];
    private InventoryDashboardDto? _dashboard;

    public InventoryDashboardControl()
    {
        if (System.Windows.Application.Current.Resources["EnterpriseInputStyle"] is Style inputStyle)
            _searchBox.Style = inputStyle;

        Content = new TextBlock { Text = "جاري التحميل...", Margin = new Thickness(24) };
        _warehouseFilter.SelectionChanged += (_, _) => OnFilterChanged(rebindContainers: true);
        _containerFilter.SelectionChanged += (_, _) => OnFilterChanged(rebindContainers: false);
        InventoryContainerFilterUi.WireSearchDebounced(_searchBox, _searchTimer, term => _ = ApplySearchAsync(term));
        Loaded += async (_, _) => await LoadAsync();
        Loaded += (_, _) => ErpDataRefreshHub.DataChanged += OnDataChanged;
        Unloaded += (_, _) =>
        {
            ErpDataRefreshHub.DataChanged -= OnDataChanged;
            _searchTimer.Stop();
        };
    }

    private void OnDataChanged(ErpDataRefreshScope scope)
    {
        if ((scope & (ErpDataRefreshScope.Inventory | ErpDataRefreshScope.Dashboard)) == 0) return;
        _ = LoadAsync();
    }

    private void OnFilterChanged(bool rebindContainers)
    {
        if (_suppressFilterChange) return;

        if (rebindContainers)
        {
            _suppressFilterChange = true;
            try
            {
                InventoryContainerFilterUi.BindContainerComboBox(
                    _containerFilter,
                    _allStock,
                    InventoryContainerFilterUi.GetSelectedId(_warehouseFilter),
                    InventoryContainerFilterUi.GetSelectedId(_containerFilter));
            }
            finally
            {
                _suppressFilterChange = false;
            }
        }

        var term = _searchBox.Text?.Trim() ?? "";
        if (term.Length >= 2)
            _ = ApplySearchAsync(term);
        else
            RenderDashboard();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;

        var dashboardResult = await InventoryUiService.Instance.GetDashboardAsync();
        if (!ApplicationResultPresenter.Present(dashboardResult) || dashboardResult.Value is null) return;

        var stockResult = await InventoryUiService.Instance.GetFabricStockAsync();
        if (!stockResult.IsSuccess || stockResult.Value is null) return;

        _dashboard = dashboardResult.Value;
        _allStock = stockResult.Value;

        var selectedWarehouseId = InventoryContainerFilterUi.GetSelectedId(_warehouseFilter);
        var selectedContainerId = InventoryContainerFilterUi.GetSelectedId(_containerFilter);

        _suppressFilterChange = true;
        try
        {
            InventoryContainerFilterUi.BindWarehouseComboBox(_warehouseFilter, _allStock, selectedWarehouseId);
            InventoryContainerFilterUi.BindContainerComboBox(
                _containerFilter,
                _allStock,
                InventoryContainerFilterUi.GetSelectedId(_warehouseFilter),
                selectedContainerId);
        }
        finally
        {
            _suppressFilterChange = false;
        }

        EnsureShell();

        var term = _searchBox.Text?.Trim() ?? "";
        if (term.Length >= 2)
            await ApplySearchAsync(term);
        else
            RenderDashboard();
    }

    private async Task ApplySearchAsync(string term)
    {
        if (!string.IsNullOrWhiteSpace(term) && term.Trim().Length >= 2 && AppServices.IsInitialized)
        {
            var warehouseId = InventoryContainerFilterUi.GetSelectedId(_warehouseFilter);
            var result = await InventoryUiService.Instance.GetFabricStockAsync(warehouseId, term.Trim());
            if (result.IsSuccess && result.Value is not null)
            {
                var containerId = InventoryContainerFilterUi.GetSelectedId(_containerFilter);
                var stock = InventoryContainerFilterUi.ApplyFilters(result.Value, null, containerId);
                RenderDashboard(stock, term.Trim());
                return;
            }
        }

        RenderDashboard();
    }

    private void EnsureShell()
    {
        if (_shellBuilt) return;

        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();
        stack.Children.Add(ErpUiFactory.SectionTitle("لوحة المخزون التنفيذية"));
        stack.Children.Add(ErpUxFactory.InfoBanner("بيانات حية من PostgreSQL — محرك المخزون المركزي"));
        stack.Children.Add(new TextBlock
        {
            Text = "ابحث باسم التوب لمعرفة في أي حاوية موجود، وكم العدد والأمتار في كل حاوية.",
            Foreground = Br("TextSecondaryBrush"),
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 8),
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial")
        });
        stack.Children.Add(InventoryContainerFilterUi.CreateFilterRow(
            "المستودع:",
            _warehouseFilter,
            "الحاوية:",
            _containerFilter,
            _searchBox));
        stack.Children.Add(_insightHost);
        stack.Children.Add(_contentPanel);
        root.Content = stack;
        Content = root;
        _shellBuilt = true;
    }

    private void RenderDashboard() =>
        RenderDashboard(GetFilteredStock(), _searchBox.Text?.Trim());

    private void RenderDashboard(IReadOnlyList<FabricStockBalanceDto> stock, string? searchTerm)
    {
        if (_dashboard is null) return;

        var warehouseId = InventoryContainerFilterUi.GetSelectedId(_warehouseFilter);
        var containerId = InventoryContainerFilterUi.GetSelectedId(_containerFilter);
        var hasSearch = !string.IsNullOrWhiteSpace(searchTerm);
        var filtered = warehouseId.HasValue || containerId.HasValue || hasSearch;

        InventoryContainerFilterUi.RenderSearchInsightPanel(_insightHost, stock, searchTerm);
        _contentPanel.Children.Clear();

        var kpis = new WrapPanel { Margin = new Thickness(0, 8, 0, 16) };
        kpis.Children.Add(InventoryContainerFilterUi.CreateMetricCard(
            "قيمة المخزون",
            $"${stock.Sum(s => s.InventoryValue):N0}",
            "\uE8C1",
            new("#FFFBEB", "#FDE68A", "#D97706", "#B45309", "#92400E")));
        kpis.Children.Add(InventoryContainerFilterUi.CreateMetricCard(
            "المستودعات",
            stock.Select(s => s.WarehouseId).Distinct().Count().ToString(),
            "\uE8B7",
            new("#EFF6FF", "#BFDBFE", "#2563EB", "#1D4ED8", "#1E40AF")));
        kpis.Children.Add(InventoryContainerFilterUi.CreateMetricCard(
            "إجمالي الأثواب",
            AppFormats.Number(stock.Sum(s => s.RollCount)),
            "\uE8CB",
            new("#F5F3FF", "#DDD6FE", "#7C3AED", "#5B21B6", "#6D28D9")));
        kpis.Children.Add(InventoryContainerFilterUi.CreateMetricCard(
            "الأمتار",
            $"{stock.Sum(s => s.TotalMeters):N0}",
            "\uE81E",
            new("#ECFEFF", "#A5F3FC", "#0891B2", "#0E7490", "#155E75")));
        kpis.Children.Add(InventoryContainerFilterUi.CreateMetricCard(
            "محجوز",
            $"{stock.Sum(s => s.ReservedMeters):N0}",
            "\uE8F1",
            new("#FFF7ED", "#FED7AA", "#EA580C", "#C2410C", "#9A3412")));
        if (!filtered)
        {
            kpis.Children.Add(InventoryContainerFilterUi.CreateMetricCard(
                "نقص مخزون",
                stock.Count(s => s.AvailableMeters > 0 && s.AvailableMeters <= 50).ToString(),
                "\uE783",
                new("#FFF1F2", "#FECDD3", "#E11D48", "#BE123C", "#9F1239")));
            kpis.Children.Add(InventoryContainerFilterUi.CreateMetricCard(
                "مناقلات",
                _dashboard.PendingTransfers.ToString(),
                "\uE898",
                new("#EEF2FF", "#C7D2FE", "#4F46E5", "#4338CA", "#3730A3")));
            kpis.Children.Add(InventoryContainerFilterUi.CreateMetricCard(
                "تنبيهات",
                _dashboard.ActiveAlerts.ToString(),
                "\uE823",
                new("#FDF2F8", "#FBCFE8", "#DB2777", "#BE185D", "#9D174D")));
        }
        _contentPanel.Children.Add(kpis);

        if (filtered)
        {
            var parts = new List<string>();
            if (hasSearch)
                parts.Add($"بحث: «{searchTerm}»");
            if (warehouseId.HasValue)
                parts.Add($"مستودع: {InventoryContainerFilterUi.GetSelectedLabel(_warehouseFilter)}");
            if (containerId.HasValue)
                parts.Add($"حاوية: {InventoryContainerFilterUi.GetSelectedLabel(_containerFilter)}");

            _contentPanel.Children.Add(new TextBlock
            {
                Text = $"عرض مخزون — {string.Join(" | ", parts)}",
                Foreground = Br("TextSecondaryBrush"),
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 8),
                FontFamily = new FontFamily("Segoe UI, Tahoma, Arial")
            });
        }

        var gridRows = hasSearch
            ? stock.OrderByDescending(s => s.TotalMeters).ToList()
            : stock.OrderByDescending(s => s.TotalMeters).Take(10).ToList();

        _contentPanel.Children.Add(ErpUiFactory.SectionTitle(hasSearch ? "نتائج البحث" : "أعلى الأقمشة"));
        _contentPanel.Children.Add(new TextBlock
        {
            Text = hasSearch
                ? "انقر نقراً مزدوجاً على أي صف لعرض تفاصيل الأتواب."
                : "انقر نقراً مزدوجاً على أي صنف لعرض تفاصيل أتوابه (الأطوال والأعداد).",
            Foreground = Br("TextSecondaryBrush"),
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 6),
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial")
        });
        _contentPanel.Children.Add(BuildStockGrid(gridRows));

        if (!hasSearch && _dashboard.RecentAlerts.Count > 0)
        {
            _contentPanel.Children.Add(ErpUiFactory.SectionTitle("تنبيهات حديثة"));
            var alertList = new StackPanel();
            foreach (var a in _dashboard.RecentAlerts)
            {
                alertList.Children.Add(new Border
                {
                    Background = Br("WarningBgBrush"),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 8),
                    Child = new TextBlock { Text = $"{a.Title}: {a.Message}", TextWrapping = TextWrapping.Wrap }
                });
            }
            _contentPanel.Children.Add(alertList);
        }
    }

    private IReadOnlyList<FabricStockBalanceDto> GetFilteredStock()
    {
        var warehouseId = InventoryContainerFilterUi.GetSelectedId(_warehouseFilter);
        var containerId = InventoryContainerFilterUi.GetSelectedId(_containerFilter);
        var term = _searchBox.Text?.Trim();
        return InventoryContainerFilterUi.ApplyFilters(_allStock, warehouseId, containerId, term);
    }

    private static DataGrid BuildStockGrid(IReadOnlyList<FabricStockBalanceDto> data)
    {
        var g = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            ItemsSource = data,
            MaxHeight = data.Count > 12 ? 520 : 300
        };
        ErpUiFactory.AddGridColumn(g, "المستودع", nameof(FabricStockBalanceDto.WarehouseName), 120, null);
        ErpUiFactory.AddGridColumn(g, "الحاوية", nameof(FabricStockBalanceDto.ContainerNumber), 100, null);
        ErpUiFactory.AddGridColumn(g, "الكود", nameof(FabricStockBalanceDto.FabricCode), 80, null);
        ErpUiFactory.AddGridColumn(g, "القماش / التوب", nameof(FabricStockBalanceDto.FabricName), "*", null);
        ErpUiFactory.AddGridColumn(g, "اللون", nameof(FabricStockBalanceDto.ColorName), 90, null);
        ErpUiFactory.AddGridColumn(g, "الأثواب", nameof(FabricStockBalanceDto.RollCount), 70, null);
        ErpUiFactory.AddGridColumn(g, "الأمتار", nameof(FabricStockBalanceDto.TotalMeters), 90, "N2");
        ErpUiFactory.AddGridColumn(g, "القيمة", nameof(FabricStockBalanceDto.InventoryValue), 100, "N2");
        g.MouseDoubleClick += (_, _) =>
        {
            if (g.SelectedItem is FabricStockBalanceDto row)
                _ = FabricRollDetailsPopup.ShowForStockRowAsync(row);
        };
        return g;
    }

    private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
}

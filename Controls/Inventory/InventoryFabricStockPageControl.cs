using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryFabricStockPageControl : UserControl
{
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true };
    private readonly ComboBox _warehouseFilter = InventoryContainerFilterUi.CreateComboBox(200);
    private readonly ComboBox _containerFilter = InventoryContainerFilterUi.CreateComboBox(200);
    private readonly TextBox _searchBox;
    private readonly WrapPanel _summaryCards = new();
    private readonly Border _insightHost = new()
    {
        Visibility = Visibility.Collapsed,
        Margin = new Thickness(0, 8, 0, 8),
        Padding = new Thickness(14, 12, 14, 12),
        CornerRadius = new CornerRadius(10),
        BorderThickness = new Thickness(1)
    };
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(280) };
    private bool _suppressFilterChange;
    private IReadOnlyList<FabricStockBalanceDto> _allStock = [];
    private string _pendingSearch = "";

    public InventoryFabricStockPageControl()
    {
        _searchBox = new TextBox
        {
            Width = 280,
            Height = 34,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 13,
            Padding = new Thickness(10, 0, 10, 0),
            ToolTip = "ابحث باسم التوب أو الكود أو اللون أو رقم الحاوية"
        };
        if (System.Windows.Application.Current.Resources["EnterpriseInputStyle"] is Style inputStyle)
            _searchBox.Style = inputStyle;

        _insightHost.Background = Hex("#EFF6FF");
        _insightHost.BorderBrush = Hex("#BFDBFE");

        var root = new DockPanel { Margin = new Thickness(16) };

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        header.Children.Add(ErpUiFactory.SectionTitle("أرصدة الأقمشة"));
        header.Children.Add(new TextBlock
        {
            Text = "ابحث باسم التوب لمعرفة في أي حاوية موجود، وكم العدد والأمتار في كل حاوية.",
            Foreground = Brushes.Gray,
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 8),
            TextWrapping = TextWrapping.Wrap
        });
        header.Children.Add(InventoryContainerFilterUi.CreateFilterRow(
            "المستودع:",
            _warehouseFilter,
            "الحاوية:",
            _containerFilter,
            _searchBox));
        header.Children.Add(_insightHost);
        _summaryCards.Margin = new Thickness(0, 0, 0, 12);
        header.Children.Add(_summaryCards);

        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        ErpUiFactory.AddGridColumn(_grid, "المستودع", nameof(FabricStockBalanceDto.WarehouseName), 120, null);
        ErpUiFactory.AddGridColumn(_grid, "الكود", nameof(FabricStockBalanceDto.FabricCode), 80, null);
        ErpUiFactory.AddGridColumn(_grid, "القماش / التوب", nameof(FabricStockBalanceDto.FabricName), "*", null);
        ErpUiFactory.AddGridColumn(_grid, "اللون", nameof(FabricStockBalanceDto.ColorName), 100, null);
        ErpUiFactory.AddGridColumn(_grid, "الحاوية", nameof(FabricStockBalanceDto.ContainerNumber), 120, null);
        ErpUiFactory.AddGridColumn(_grid, "الأثواب", nameof(FabricStockBalanceDto.RollCount), 70, null);
        ErpUiFactory.AddGridColumn(_grid, "الإجمالي م", nameof(FabricStockBalanceDto.TotalMeters), 90, "N2");
        ErpUiFactory.AddGridColumn(_grid, "المحجوز م", nameof(FabricStockBalanceDto.ReservedMeters), 90, "N2");
        ErpUiFactory.AddGridColumn(_grid, "المتاح م", nameof(FabricStockBalanceDto.AvailableMeters), 90, "N2");
        ErpUiFactory.AddGridColumn(_grid, "القيمة $", nameof(FabricStockBalanceDto.InventoryValue), 100, "N2");

        root.Children.Add(_grid);
        Content = root;

        _grid.MouseDoubleClick += (_, _) => _ = ShowRollDetailsAsync();
        _warehouseFilter.SelectionChanged += (_, _) => OnFilterChanged(rebindContainers: true);
        _containerFilter.SelectionChanged += (_, _) => OnFilterChanged(rebindContainers: false);
        _searchBox.TextChanged += (_, _) =>
        {
            _pendingSearch = _searchBox.Text ?? "";
            _searchTimer.Stop();
            _searchTimer.Start();
        };
        _searchTimer.Tick += async (_, _) =>
        {
            _searchTimer.Stop();
            await ApplySearchAsync(_pendingSearch);
        };

        Loaded += async (_, _) => await LoadAsync();
        ErpDataRefreshHub.DataChanged += OnDataChanged;
        Unloaded += (_, _) =>
        {
            ErpDataRefreshHub.DataChanged -= OnDataChanged;
            _searchTimer.Stop();
        };
    }

    private void OnDataChanged(ErpDataRefreshScope scope)
    {
        if ((scope & ErpDataRefreshScope.Inventory) == 0) return;
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
            ApplyFilter();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;

        var result = await InventoryUiService.Instance.GetFabricStockAsync();
        if (!result.IsSuccess || result.Value is null) return;

        _allStock = result.Value;
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

        ApplyFilter();
    }

    private async Task ApplySearchAsync(string term)
    {
        // For large catalogs, prefer server-side search when the term is meaningful.
        if (!string.IsNullOrWhiteSpace(term) && term.Trim().Length >= 2 && AppServices.IsInitialized)
        {
            var warehouseId = InventoryContainerFilterUi.GetSelectedId(_warehouseFilter);
            var result = await InventoryUiService.Instance.GetFabricStockAsync(warehouseId, term.Trim());
            if (result.IsSuccess && result.Value is not null)
            {
                var containerId = InventoryContainerFilterUi.GetSelectedId(_containerFilter);
                var stock = InventoryContainerFilterUi.ApplyFilters(result.Value, null, containerId);
                BindResults(stock, term.Trim());
                return;
            }
        }

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var warehouseId = InventoryContainerFilterUi.GetSelectedId(_warehouseFilter);
        var containerId = InventoryContainerFilterUi.GetSelectedId(_containerFilter);
        var term = _searchBox.Text?.Trim();
        var stock = InventoryContainerFilterUi.ApplyFilters(_allStock, warehouseId, containerId, term);
        BindResults(stock, term);
    }

    private void BindResults(IReadOnlyList<FabricStockBalanceDto> stock, string? searchTerm)
    {
        _grid.ItemsSource = stock;
        InventoryContainerFilterUi.PopulateStockSummaryCards(_summaryCards, stock);
        RenderSearchInsight(stock, searchTerm);
    }

    private void RenderSearchInsight(IReadOnlyList<FabricStockBalanceDto> stock, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || stock.Count == 0)
        {
            _insightHost.Visibility = Visibility.Collapsed;
            _insightHost.Child = null;
            return;
        }

        var insights = InventoryContainerFilterUi.BuildSearchInsights(stock);
        if (insights.Count == 0)
        {
            _insightHost.Visibility = Visibility.Collapsed;
            _insightHost.Child = null;
            return;
        }

        var root = new StackPanel();
        root.Children.Add(new TextBlock
        {
            Text = $"نتائج البحث عن «{searchTerm}» — {insights.Count} توب في {stock.Select(s => s.ContainerId).Distinct().Count()} حاوية",
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Foreground = Hex("#1E3A8A"),
            Margin = new Thickness(0, 0, 0, 8)
        });

        foreach (var insight in insights.Take(8))
        {
            var card = new Border
            {
                Background = Brushes.White,
                BorderBrush = Hex("#DBEAFE"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var body = new StackPanel();
            body.Children.Add(new TextBlock
            {
                Text = $"{insight.FabricName} ({insight.FabricCode})",
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Foreground = Hex("#0F172A")
            });
            body.Children.Add(new TextBlock
            {
                Text = $"موجود في {insight.ContainerCount} حاوية • {AppFormats.Number(insight.TotalRolls)} ثوب • {AppFormats.Number(insight.TotalMeters, 0)} م • متاح {AppFormats.Number(insight.AvailableMeters, 0)} م",
                FontSize = 11,
                Foreground = Hex("#475569"),
                Margin = new Thickness(0, 4, 0, 8)
            });

            foreach (var loc in insight.Locations.Take(12))
            {
                var line = new TextBlock
                {
                    Text = $"• حاوية {loc.ContainerNumber} — {loc.WarehouseName} — {AppFormats.Number(loc.RollCount)} ثوب — {AppFormats.Number(loc.TotalMeters, 0)} م" +
                           (loc.ColorCount > 1 ? $" — {loc.ColorCount} ألوان" : ""),
                    FontSize = 12,
                    Foreground = Hex("#1E293B"),
                    Margin = new Thickness(4, 0, 0, 2)
                };
                body.Children.Add(line);
            }

            if (insight.Locations.Count > 12)
            {
                body.Children.Add(new TextBlock
                {
                    Text = $"… و {insight.Locations.Count - 12} حاوية إضافية في الجدول أدناه",
                    FontSize = 11,
                    Foreground = Hex("#64748B"),
                    Margin = new Thickness(4, 4, 0, 0)
                });
            }

            card.Child = body;
            root.Children.Add(card);
        }

        if (insights.Count > 8)
        {
            root.Children.Add(new TextBlock
            {
                Text = $"… و {insights.Count - 8} توب إضافي ظاهر في الجدول",
                FontSize = 11,
                Foreground = Hex("#64748B")
            });
        }

        _insightHost.Child = root;
        _insightHost.Visibility = Visibility.Visible;
    }

    private async Task ShowRollDetailsAsync()
    {
        if (_grid.SelectedItem is FabricStockBalanceDto row)
            await FabricRollDetailsPopup.ShowForStockRowAsync(row);
    }

    private static SolidColorBrush Hex(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex)!);
}

public sealed class InventoryTransferListPageControl : UserControl
{
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true };

    public InventoryTransferListPageControl()
    {
        var root = new DockPanel { Margin = new Thickness(16) };
        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        header.Children.Add(ErpUiFactory.SectionTitle("المناقلات"));
        var btn = new Button
        {
            Content = "مناقلة جديدة",
            Style = (Style)System.Windows.Application.Current.Resources["SecondaryButtonStyle"]!,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        btn.Click += (_, _) => InventoryPopupService.ShowTransferWizard();
        header.Children.Add(btn);
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        ErpUiFactory.AddGridColumn(_grid, "الرقم", nameof(StockTransferListDto.Number), 120, null);
        ErpUiFactory.AddGridColumn(_grid, "من", nameof(StockTransferListDto.FromWarehouse), "*", null);
        ErpUiFactory.AddGridColumn(_grid, "إلى", nameof(StockTransferListDto.ToWarehouse), "*", null);
        ErpUiFactory.AddGridColumn(_grid, "الحالة", nameof(StockTransferListDto.Status), 100, null);
        ErpUiFactory.AddGridColumn(_grid, "التاريخ", nameof(StockTransferListDto.Date), 120, null);

        _grid.MouseDoubleClick += (_, _) =>
        {
            if (_grid.SelectedItem is StockTransferListDto t)
                InventoryPopupService.ShowTransferWizard(transferId: t.Id);
        };

        root.Children.Add(_grid);
        Content = root;
        Loaded += async (_, _) => await LoadAsync();
        InventoryListRefreshHub.RefreshRequested += (_, _) => _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;
        var result = await InventoryUiService.Instance.GetTransfersAsync();
        if (result.IsSuccess && result.Value is not null)
            _grid.ItemsSource = result.Value;
    }
}

public sealed class InventoryStocktakeListPageControl : UserControl
{
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true };

    public InventoryStocktakeListPageControl()
    {
        var root = new DockPanel { Margin = new Thickness(16) };
        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        header.Children.Add(ErpUiFactory.SectionTitle("جلسات الجرد"));
        var btn = new Button
        {
            Content = "جرد جديد",
            Style = (Style)System.Windows.Application.Current.Resources["SecondaryButtonStyle"]!,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        btn.Click += (_, _) => InventoryPopupService.ShowStocktakeWizard();
        header.Children.Add(btn);
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        ErpUiFactory.AddGridColumn(_grid, "الرقم", nameof(StocktakeListDto.SessionNumber), 120, null);
        ErpUiFactory.AddGridColumn(_grid, "المستودع", nameof(StocktakeListDto.WarehouseName), "*", null);
        ErpUiFactory.AddGridColumn(_grid, "المسؤول", nameof(StocktakeListDto.Responsible), 120, null);
        ErpUiFactory.AddGridColumn(_grid, "الحالة", nameof(StocktakeListDto.Status), 100, null);
        ErpUiFactory.AddGridColumn(_grid, "التاريخ", nameof(StocktakeListDto.Date), 120, null);

        _grid.MouseDoubleClick += (_, _) =>
        {
            if (_grid.SelectedItem is StocktakeListDto s)
                InventoryPopupService.ShowStocktakeWizard(sessionId: s.Id);
        };

        root.Children.Add(_grid);
        Content = root;
        Loaded += async (_, _) => await LoadAsync();
        InventoryListRefreshHub.RefreshRequested += (_, _) => _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;
        var result = await InventoryUiService.Instance.GetStocktakeSessionsAsync();
        if (result.IsSuccess && result.Value is not null) _grid.ItemsSource = result.Value;
    }
}

public sealed class InventoryOpeningStockPageControl : UserControl
{
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true };

    public InventoryOpeningStockPageControl()
    {
        var root = new DockPanel { Margin = new Thickness(16) };
        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        header.Children.Add(ErpUiFactory.SectionTitle("مواد أول المدة"));
        var btn = new Button
        {
            Content = "مستند جديد",
            Style = (Style)System.Windows.Application.Current.Resources["SecondaryButtonStyle"]!,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        btn.Click += (_, _) =>
        {
            InventoryNavigationContext.BeginCreateOpeningStock();
            NavigationStateManager.Instance.NavigateTo(AppModule.Inventory, "OpeningStockForm");
        };
        header.Children.Add(btn);
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        ErpUiFactory.AddGridColumn(_grid, "الرقم", nameof(OpeningStockListDto.DocumentNumber), 120, null);
        ErpUiFactory.AddGridColumn(_grid, "المستودع", nameof(OpeningStockListDto.WarehouseName), "*", null);
        ErpUiFactory.AddGridColumn(_grid, "التاريخ", nameof(OpeningStockListDto.OpeningDate), 120, null);
        ErpUiFactory.AddGridColumn(_grid, "القيمة", nameof(OpeningStockListDto.TotalValue), 100, "N2");
        ErpUiFactory.AddGridColumn(_grid, "الحالة", nameof(OpeningStockListDto.Status), 100, null);
        root.Children.Add(_grid);
        Content = root;
        Loaded += async (_, _) => await LoadAsync();
        InventoryListRefreshHub.RefreshRequested += (_, _) => _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;
        var result = await InventoryUiService.Instance.GetOpeningStockDocumentsAsync();
        if (result.IsSuccess && result.Value is not null) _grid.ItemsSource = result.Value;
    }
}

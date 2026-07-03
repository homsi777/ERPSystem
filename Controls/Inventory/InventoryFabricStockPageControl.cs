using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryFabricStockPageControl : UserControl
{
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true };

    public InventoryFabricStockPageControl()
    {
        var root = new DockPanel { Margin = new Thickness(16) };
        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        header.Children.Add(ErpUiFactory.SectionTitle("أرصدة الأقمشة"));
        header.Children.Add(new TextBlock
        {
            Text = "أرصدة حية محسوبة من حركات المخزون — لا بيانات تجريبية",
            Foreground = System.Windows.Media.Brushes.Gray, Margin = new Thickness(0, 0, 0, 8)
        });
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        ErpUiFactory.AddGridColumn(_grid, "المستودع", nameof(FabricStockBalanceDto.WarehouseName), 120, null);
        ErpUiFactory.AddGridColumn(_grid, "الكود", nameof(FabricStockBalanceDto.FabricCode), 80, null);
        ErpUiFactory.AddGridColumn(_grid, "القماش", nameof(FabricStockBalanceDto.FabricName), "*", null);
        ErpUiFactory.AddGridColumn(_grid, "اللون", nameof(FabricStockBalanceDto.ColorName), 100, null);
        ErpUiFactory.AddGridColumn(_grid, "الحاوية", nameof(FabricStockBalanceDto.ContainerNumber), 100, null);
        ErpUiFactory.AddGridColumn(_grid, "Rolls", nameof(FabricStockBalanceDto.RollCount), 60, null);
        ErpUiFactory.AddGridColumn(_grid, "الإجمالي", nameof(FabricStockBalanceDto.TotalMeters), 90, null);
        ErpUiFactory.AddGridColumn(_grid, "المحجوز", nameof(FabricStockBalanceDto.ReservedMeters), 90, null);
        ErpUiFactory.AddGridColumn(_grid, "المتاح", nameof(FabricStockBalanceDto.AvailableMeters), 90, null);
        ErpUiFactory.AddGridColumn(_grid, "القيمة $", nameof(FabricStockBalanceDto.InventoryValue), 100, null);

        root.Children.Add(_grid);
        Content = root;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;
        var result = await InventoryUiService.Instance.GetFabricStockAsync();
        if (result.IsSuccess && result.Value is not null)
            _grid.ItemsSource = result.Value;
    }
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
        ErpUiFactory.AddGridColumn(_grid, "القيمة", nameof(OpeningStockListDto.TotalValue), 100, null);
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

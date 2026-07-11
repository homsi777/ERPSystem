using ERPSystem.Application.DTOs.Catalog;
using ERPSystem.Controls;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ERPSystem.Diagnostics.Performance;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryFabricCategoriesPageControl : UserControl
{
    private readonly ImportedFabricClassificationPanel _panel = new();

    public InventoryFabricCategoriesPageControl()
    {
        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var title = ErpUiFactory.SectionTitle("تصنيفات الأقمشة المستوردة");
        Grid.SetRow(title, 0);
        root.Children.Add(title);

        var banner = ErpUxFactory.InfoBanner(
            "التصنيفات تُنشأ تلقائياً عند استيراد حاوية من الصين (DPL). يمكنك تعديل الأسماء فقط — لا إضافة يدوية.");
        Grid.SetRow(banner, 1);
        root.Children.Add(banner);

        Grid.SetRow(_panel, 2);
        root.Children.Add(_panel);
        Content = root;
    }
}

internal sealed class ContainerFilterOption
{
    public Guid? Id { get; init; }
    public string Label { get; init; } = "";
}

internal sealed class ImportedFabricClassificationPanel : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private readonly ComboBox _containerFilter = new()
    {
        Width = 200,
        Height = ErpDesignTokens.ControlHeight,
        DisplayMemberPath = nameof(ContainerFilterOption.Label),
        FontSize = ErpDesignTokens.FontBody - 1
    };
    private bool _isLoading;
    private bool _suppressFilterChange;

    public ImportedFabricClassificationPanel()
    {
        VerticalAlignment = VerticalAlignment.Stretch;
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _page.SetEmptyState(
            "لا توجد تصنيفات بعد — ستظهر تلقائياً بعد استيراد حاوية من الصين",
            null,
            "\uECA5");
        _containerFilter.Style = (Style)System.Windows.Application.Current.Resources["EnterpriseComboBoxStyle"]!;
        _page.SetFilterExtras(_containerFilter);
        _containerFilter.SelectionChanged += async (_, _) =>
        {
            if (_suppressFilterChange) return;
            await LoadAsync();
        };

        var g = _page.Grid;
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        foreach (var (h, p, w, fmt) in new (string, string, object, string?)[]
        {
            ("الحاوية", nameof(ImportedFabricClassificationDto.ContainerNumber), 110, null),
            ("كود التوب", nameof(ImportedFabricClassificationDto.FabricCode), 90, null),
            ("التصنيف", nameof(ImportedFabricClassificationDto.NameAr), "*", null),
            ("اللون", nameof(ImportedFabricClassificationDto.ColorNameAr), 100, null),
            ("التوب", nameof(ImportedFabricClassificationDto.RollCount), 70, null),
            ("الأمتار", nameof(ImportedFabricClassificationDto.LengthMeters), 90, "N2")
        })
            ErpUiFactory.AddGridColumn(g, h, p, w, fmt);

        g.MouseDoubleClick += (_, _) =>
        {
            if (g.SelectedItem is ImportedFabricClassificationDto row)
                InventoryCatalogPopupService.ShowEditClassification(row);
        };

        g.PreviewMouseRightButtonDown += (_, e) =>
        {
            if (FindRow(g, e) is not DataGridRow dgRow || dgRow.Item is not ImportedFabricClassificationDto row)
                return;
            e.Handled = true;
            dgRow.IsSelected = true;
            ShowMenu(row, g);
        };

        Content = _page;
        Loaded += async (_, _) =>
        {
            await LoadContainerFilterAsync();
            await LoadAsync();
        };
        Unloaded += OnUnloaded;
        InventoryCatalogListRefreshHub.RefreshRequested += OnRefreshRequested;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) =>
        InventoryCatalogListRefreshHub.RefreshRequested -= OnRefreshRequested;

    private async void OnRefreshRequested(object? sender, EventArgs e)
    {
        await LoadContainerFilterAsync();
        await LoadAsync();
    }

    private async Task LoadContainerFilterAsync()
    {
        var result = await InventoryCatalogUiService.Instance.GetImportedContainerFiltersAsync();
        if (!result.IsSuccess || result.Value is null) return;

        var selectedId = (_containerFilter.SelectedItem as ContainerFilterOption)?.Id;
        var items = new List<ContainerFilterOption>
        {
            new() { Id = null, Label = "كل الحاويات" }
        };
        items.AddRange(result.Value.Select(c =>
            new ContainerFilterOption { Id = c.Id, Label = $"{c.ContainerNumber} ({c.FabricTypeCount})" }));

        _suppressFilterChange = true;
        try
        {
            _containerFilter.ItemsSource = items;
            _containerFilter.SelectedItem = selectedId.HasValue
                ? items.FirstOrDefault(i => i.Id == selectedId) ?? items[0]
                : items[0];
        }
        finally
        {
            _suppressFilterChange = false;
        }
    }

    private async Task LoadAsync()
    {
        using var perfScope = ScreenLoadProfiler.Begin("Inventory.Categories");
        if (_isLoading || !AppServices.IsInitialized) return;

        _isLoading = true;
        _page.SetLoadingState(true);
        try
        {
            Guid? containerId = (_containerFilter.SelectedItem as ContainerFilterOption)?.Id;
            var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => InventoryCatalogUiService.Instance.GetImportedClassificationsAsync(containerId));
        perfScope?.IncrementServiceCalls();
            _page.BindData(result.IsSuccess && result.Value is not null
                ? result.Value.Cast<object>().ToList()
                : []);
        }
        finally
        {
            _page.SetLoadingState(false);
            _isLoading = false;
        }
    }

    private static void ShowMenu(ImportedFabricClassificationDto row, DataGrid grid)
    {
        var menu = new ContextMenu { FlowDirection = FlowDirection.RightToLeft, MinWidth = 200 };
        menu.Items.Add(new MenuItem { Header = row.DisplayLabel, IsEnabled = false, FontWeight = FontWeights.SemiBold });
        menu.Items.Add(new Separator());
        var edit = new MenuItem { Header = "تعديل التصنيف", Padding = new Thickness(12, 8, 12, 8) };
        edit.Click += (_, _) => InventoryCatalogPopupService.ShowEditClassification(row);
        menu.Items.Add(edit);
        menu.PlacementTarget = grid;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    private static DataGridRow? FindRow(DataGrid grid, MouseButtonEventArgs e)
    {
        var dep = (DependencyObject)e.OriginalSource;
        while (dep != null)
        {
            if (dep is DataGridRow row) return row;
            dep = VisualTreeHelper.GetParent(dep);
        }
        return null;
    }
}

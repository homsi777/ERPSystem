using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using ERPSystem.Diagnostics.Performance;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryOperationsCenterControl : UserControl
{
    private InventoryOperationsCenterDto? _data;
    private TabControl? _detailTabs;
    private Guid? _popupWarehouseId;
    private string? _popupTab;

    public InventoryOperationsCenterControl()
    {
        Content = BuildLoadingSkeleton();
        Loaded += OnLoaded;
    }

    /// <summary>تحميل مركز العمل داخل نافذة منبثقة — بدون تنقل.</summary>
    public void InitializeForPopup(Guid warehouseId, string? initialTab = null)
    {
        _popupWarehouseId = warehouseId;
        _popupTab = initialTab;
        if (IsLoaded)
            _ = LoadAsync(warehouseId, initialTab);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        Guid? id;
        string? tab;
        if (_popupWarehouseId.HasValue)
        {
            id = _popupWarehouseId;
            tab = _popupTab;
            _popupWarehouseId = null;
            _popupTab = null;
        }
        else
        {
            (id, tab) = InventoryNavigationContext.TakeWorkspaceContext();
        }

        if (!id.HasValue)
        {
            Content = EmptyState("لم يُحدد مستودع", "افتح مستودعاً من قائمة المستودعات.");
            return;
        }
        await LoadAsync(id.Value, tab);
    }

    private async Task LoadAsync(Guid warehouseId, string? initialTab)
    {
        if (!AppServices.IsInitialized)
        {
            Content = EmptyState("غير متصل", "تعذر الاتصال بخدمات التطبيق.");
            return;
        }

        Content = BuildLoadingSkeleton();
        using var perfScope = ScreenLoadProfiler.Begin("Inventory.OperationsCenter");
        var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => InventoryUiService.Instance.GetOperationsCenterAsync(warehouseId));
        perfScope?.IncrementServiceCalls();
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
        {
            Content = EmptyState("تعذر التحميل", "لم يتم العثور على بيانات المستودع في PostgreSQL.");
            return;
        }

        _data = result.Value;
        RenderWorkspace(initialTab);
    }

    private void RenderWorkspace(string? initialTab)
    {
        if (_data is null) return;
        var w = _data.Warehouse;

        var host = WarehouseExecutiveWorkspaceBuilder.Build(
            _data,
            tab => SelectDetailTab(tab),
            () => WarehouseContextMenuService.Show(w, this));

        if (host is ScrollViewer sv && sv.Content is StackPanel sp)
        {
            foreach (var child in sp.Children)
            {
                if (child is TabControl tc)
                {
                    _detailTabs = tc;
                    break;
                }
            }
        }

        Content = host;
        if (!string.IsNullOrWhiteSpace(initialTab))
            SelectDetailTab(MapTabKey(initialTab));
    }

    private void SelectDetailTab(string key)
    {
        if (_detailTabs is null) return;
        foreach (TabItem item in _detailTabs.Items)
        {
            if (item.Tag is string k && k.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                _detailTabs.SelectedItem = item;
                return;
            }
        }
    }

    private static string MapTabKey(string key) => key switch
    {
        "Stock" or "stock" => "Stock",
        "Rolls" => "Rolls",
        "Locations" => "Locations",
        "Movements" => "Movements",
        "Audit" => "Audit",
        "Timeline" => "Timeline",
        _ => "Stock"
    };

    private static UIElement BuildLoadingSkeleton()
    {
        var sp = new StackPanel { Margin = new Thickness(24), MaxWidth = 1200 };
        sp.Children.Add(new TextBlock
        {
            Text = "جاري تحميل مركز عمل المستودع...",
            FontSize = 16,
            Foreground = Br("TextSecondaryBrush"),
            Margin = new Thickness(0, 0, 0, 16)
        });
        var shimmer = new WrapPanel();
        for (var i = 0; i < 6; i++)
        {
            shimmer.Children.Add(new Border
            {
                Width = 150, Height = 90,
                Background = Br("SurfaceAltBrush"),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 12, 12)
            });
        }
        sp.Children.Add(shimmer);
        sp.Children.Add(new Border
        {
            Height = 200,
            Background = Br("SurfaceAltBrush"),
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(0, 8, 0, 0)
        });
        return new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    private static UIElement EmptyState(string title, string hint) => new Border
    {
        Margin = new Thickness(32),
        Padding = new Thickness(32),
        Background = Br("SurfaceBrush"),
        CornerRadius = new CornerRadius(12),
        Child = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.SemiBold },
                new TextBlock { Text = hint, Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap, Foreground = Br("TextMutedBrush") }
            }
        }
    };

    private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
}

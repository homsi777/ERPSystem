using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryDashboardControl : UserControl
{
    public InventoryDashboardControl()
    {
        Content = new TextBlock { Text = "جاري التحميل...", Margin = new Thickness(24) };
        Loaded += async (_, _) => await LoadAsync();
        Loaded += (_, _) => ErpDataRefreshHub.DataChanged += OnDataChanged;
        Unloaded += (_, _) => ErpDataRefreshHub.DataChanged -= OnDataChanged;
    }

    private void OnDataChanged(ErpDataRefreshScope scope)
    {
        if ((scope & (ErpDataRefreshScope.Inventory | ErpDataRefreshScope.Dashboard)) == 0) return;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;
        var result = await InventoryUiService.Instance.GetDashboardAsync();
        if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;

        var d = result.Value;
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();
        stack.Children.Add(ErpUiFactory.SectionTitle("لوحة المخزون التنفيذية"));
        stack.Children.Add(ErpUxFactory.InfoBanner("بيانات حية من PostgreSQL — محرك المخزون المركزي"));

        var kpis = new WrapPanel { Margin = new Thickness(0, 12, 0, 16) };
        foreach (var (label, value, icon) in new (string, string, string)[]
        {
            ("قيمة المخزون", $"${d.TotalInventoryValue:N0}", "\uE8C1"),
            ("المستودعات", d.WarehouseCount.ToString(), "\uE8B7"),
            ("إجمالي Rolls", d.TotalRolls.ToString(), "\uE8CB"),
            ("الأمتار", $"{d.TotalMeters:N0}", "\uE8AB"),
            ("محجوز", $"{d.ReservedMeters:N0}", "\uE8F1"),
            ("نقص مخزون", d.LowStockCount.ToString(), "\uE783"),
            ("مناقلات", d.PendingTransfers.ToString(), "\uE898"),
            ("تنبيهات", d.ActiveAlerts.ToString(), "\uE823"),
        })
            kpis.Children.Add(KpiCard(label, value, icon));
        stack.Children.Add(kpis);

        stack.Children.Add(ErpUiFactory.SectionTitle("أعلى الأقمشة"));
        stack.Children.Add(BuildStockGrid(d.TopFabrics));

        if (d.RecentAlerts.Count > 0)
        {
            stack.Children.Add(ErpUiFactory.SectionTitle("تنبيهات حديثة"));
            var alertList = new StackPanel();
            foreach (var a in d.RecentAlerts)
            {
                alertList.Children.Add(new Border
                {
                    Background = Br("WarningBgBrush"), CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 8),
                    Child = new TextBlock { Text = $"{a.Title}: {a.Message}", TextWrapping = TextWrapping.Wrap }
                });
            }
            stack.Children.Add(alertList);
        }

        root.Content = stack;
        Content = root;
    }

    private static Border KpiCard(string label, string value, string icon) => new()
    {
        Width = 160, Margin = new Thickness(0, 0, 12, 12), Padding = new Thickness(16),
        Background = Br("SurfaceBrush"), CornerRadius = new CornerRadius(10),
        BorderBrush = Br("BorderBrush"), BorderThickness = new Thickness(1),
        Child = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = icon, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 20 },
                new TextBlock { Text = value, FontSize = 20, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 4) },
                new TextBlock { Text = label, Foreground = Br("TextSecondaryBrush"), FontSize = 12 }
            }
        }
    };

    private static DataGrid BuildStockGrid(IReadOnlyList<FabricStockBalanceDto> data)
    {
        var g = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, ItemsSource = data, MaxHeight = 300 };
        ErpUiFactory.AddGridColumn(g, "المستودع", nameof(FabricStockBalanceDto.WarehouseName), 120, null);
        ErpUiFactory.AddGridColumn(g, "القماش", nameof(FabricStockBalanceDto.FabricName), "*", null);
        ErpUiFactory.AddGridColumn(g, "الأمتار", nameof(FabricStockBalanceDto.TotalMeters), 90, null);
        ErpUiFactory.AddGridColumn(g, "القيمة", nameof(FabricStockBalanceDto.InventoryValue), 100, null);
        return g;
    }

    private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
}

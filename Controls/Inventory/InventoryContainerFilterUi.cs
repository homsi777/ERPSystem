using ERPSystem.Application.DTOs.Inventory;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Inventory;

internal sealed class InventoryFilterOption
{
    public Guid? Id { get; init; }
    public string Label { get; init; } = "";
}

internal readonly record struct InventoryMetricCardTheme(
    string Background,
    string Border,
    string Icon,
    string Value,
    string Label);

internal static class InventoryContainerFilterUi
{
    public static ComboBox CreateComboBox(double width = 230)
    {
        var combo = new ComboBox
        {
            Width = width,
            Height = 34,
            DisplayMemberPath = nameof(InventoryFilterOption.Label),
            VerticalContentAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 13
        };

        if (System.Windows.Application.Current.Resources["EnterpriseComboBoxStyle"] is Style style)
            combo.Style = style;

        return combo;
    }

    public static IReadOnlyList<InventoryFilterOption> BuildWarehouseOptions(IEnumerable<FabricStockBalanceDto> stock)
    {
        var items = new List<InventoryFilterOption>
        {
            new() { Id = null, Label = "كل المستودعات" }
        };

        items.AddRange(stock
            .GroupBy(s => s.WarehouseId)
            .Select(g =>
            {
                var first = g.First();
                return new InventoryFilterOption
                {
                    Id = g.Key,
                    Label = $"{first.WarehouseName} ({g.Count()} صنف)"
                };
            })
            .OrderBy(o => o.Label));

        return items;
    }

    public static IReadOnlyList<InventoryFilterOption> BuildContainerOptions(IEnumerable<FabricStockBalanceDto> stock)
    {
        var items = new List<InventoryFilterOption>
        {
            new() { Id = null, Label = "كل الحاويات" }
        };

        items.AddRange(stock
            .Where(s => s.ContainerId != Guid.Empty)
            .GroupBy(s => s.ContainerId)
            .Select(g =>
            {
                var first = g.First();
                return new InventoryFilterOption
                {
                    Id = g.Key,
                    Label = $"{first.ContainerNumber} ({g.Count()} صنف)"
                };
            })
            .OrderByDescending(o => o.Label));

        return items;
    }

    public static IReadOnlyList<FabricStockBalanceDto> ApplyFilters(
        IReadOnlyList<FabricStockBalanceDto> stock,
        Guid? warehouseId,
        Guid? containerId)
    {
        IEnumerable<FabricStockBalanceDto> query = stock;
        if (warehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == warehouseId.Value);
        if (containerId.HasValue)
            query = query.Where(s => s.ContainerId == containerId.Value);
        return query.ToList();
    }

    public static IReadOnlyList<FabricStockBalanceDto> ScopeForWarehouse(
        IReadOnlyList<FabricStockBalanceDto> stock,
        Guid? warehouseId) =>
        warehouseId.HasValue
            ? stock.Where(s => s.WarehouseId == warehouseId.Value).ToList()
            : stock;

    public static void BindWarehouseComboBox(
        ComboBox combo,
        IReadOnlyList<FabricStockBalanceDto> stock,
        Guid? selectedId = null)
    {
        var items = BuildWarehouseOptions(stock);
        combo.ItemsSource = items;
        combo.SelectedItem = selectedId.HasValue
            ? items.FirstOrDefault(i => i.Id == selectedId) ?? items[0]
            : items[0];
    }

    public static void BindContainerComboBox(
        ComboBox combo,
        IReadOnlyList<FabricStockBalanceDto> stock,
        Guid? warehouseId,
        Guid? selectedId = null)
    {
        var scoped = ScopeForWarehouse(stock, warehouseId);
        var items = BuildContainerOptions(scoped);
        combo.ItemsSource = items;
        combo.SelectedItem = selectedId.HasValue
            ? items.FirstOrDefault(i => i.Id == selectedId) ?? items[0]
            : items[0];
    }

    public static Guid? GetSelectedId(ComboBox combo) =>
        (combo.SelectedItem as InventoryFilterOption)?.Id;

    public static string? GetSelectedLabel(ComboBox combo) =>
        (combo.SelectedItem as InventoryFilterOption)?.Label;

    public static Border CreateMetricCard(
        string label,
        string value,
        string icon,
        InventoryMetricCardTheme theme,
        double width = 168) => new()
    {
        Width = width,
        Margin = new Thickness(0, 0, 12, 12),
        Padding = new Thickness(14, 12, 14, 12),
        Background = HexBrush(theme.Background),
        BorderBrush = HexBrush(theme.Border),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10),
        Child = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = icon,
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 16,
                    Foreground = HexBrush(theme.Icon)
                },
                new TextBlock
                {
                    Text = value,
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    Foreground = HexBrush(theme.Value),
                    Margin = new Thickness(0, 8, 0, 4),
                    FontFamily = new FontFamily("Segoe UI, Tahoma, Arial")
                },
                new TextBlock
                {
                    Text = label,
                    Foreground = HexBrush(theme.Label),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    FontFamily = new FontFamily("Segoe UI, Tahoma, Arial")
                }
            }
        }
    };

    public static WrapPanel BuildStockSummaryCards(IReadOnlyList<FabricStockBalanceDto> stock)
    {
        var panel = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        PopulateStockSummaryCards(panel, stock);
        return panel;
    }

    public static void PopulateStockSummaryCards(WrapPanel panel, IReadOnlyList<FabricStockBalanceDto> stock)
    {
        panel.Children.Clear();
        panel.Children.Add(CreateMetricCard(
            "إجمالي Rolls",
            stock.Sum(s => s.RollCount).ToString("N0"),
            "\uE8CB",
            new("#F5F3FF", "#DDD6FE", "#7C3AED", "#5B21B6", "#6D28D9")));
        panel.Children.Add(CreateMetricCard(
            "الأمتار",
            $"{stock.Sum(s => s.TotalMeters):N0}",
            "\uE81E",
            new("#EFF6FF", "#BFDBFE", "#2563EB", "#1D4ED8", "#1E40AF")));
        panel.Children.Add(CreateMetricCard(
            "المتاح",
            $"{stock.Sum(s => s.AvailableMeters):N0}",
            "\uE73E",
            new("#ECFDF5", "#A7F3D0", "#059669", "#047857", "#065F46")));
        panel.Children.Add(CreateMetricCard(
            "قيمة المخزون",
            $"${stock.Sum(s => s.InventoryValue):N0}",
            "\uE8C1",
            new("#FFFBEB", "#FDE68A", "#D97706", "#B45309", "#92400E")));
    }

    public static StackPanel CreateFilterRow(
        string warehouseLabel,
        ComboBox warehouseCombo,
        string containerLabel,
        ComboBox containerCombo)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 4)
        };

        row.Children.Add(CreateFilterLabel(warehouseLabel));
        row.Children.Add(warehouseCombo);
        row.Children.Add(CreateFilterLabel(containerLabel, new Thickness(16, 0, 10, 0)));
        row.Children.Add(containerCombo);
        return row;
    }

    private static TextBlock CreateFilterLabel(string text, Thickness? margin = null) => new()
    {
        Text = text,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = margin ?? new Thickness(0, 0, 10, 0),
        FontWeight = FontWeights.SemiBold,
        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")!),
        FontFamily = new FontFamily("Segoe UI, Tahoma, Arial")
    };

    private static SolidColorBrush HexBrush(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex)!);
}

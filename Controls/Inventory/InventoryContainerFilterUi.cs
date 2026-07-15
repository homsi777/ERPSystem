using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

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
        Guid? containerId,
        string? search = null)
    {
        IEnumerable<FabricStockBalanceDto> query = stock;
        if (warehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == warehouseId.Value);
        if (containerId.HasValue)
            query = query.Where(s => s.ContainerId == containerId.Value);

        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(s =>
                ContainsIgnoreCase(s.FabricName, term) ||
                ContainsIgnoreCase(s.FabricCode, term) ||
                ContainsIgnoreCase(s.ColorName, term) ||
                ContainsIgnoreCase(s.ContainerNumber, term) ||
                ContainsIgnoreCase(s.WarehouseName, term));
        }

        return query
            .OrderBy(s => s.FabricName)
            .ThenBy(s => s.ColorName)
            .ThenBy(s => s.ContainerNumber)
            .ToList();
    }

    public static bool ContainsIgnoreCase(string? value, string term) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(term, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Smart insight: for a search hit, summarize where the fabric lives across containers.
    /// </summary>
    public static IReadOnlyList<FabricSearchInsight> BuildSearchInsights(
        IReadOnlyList<FabricStockBalanceDto> filteredStock)
    {
        return filteredStock
            .GroupBy(s => new { s.FabricItemId, s.FabricCode, s.FabricName })
            .Select(g =>
            {
                var containers = g
                    .GroupBy(x => new { x.ContainerId, x.ContainerNumber, x.WarehouseId, x.WarehouseName })
                    .Select(cg => new FabricContainerLocation
                    {
                        ContainerId = cg.Key.ContainerId,
                        ContainerNumber = cg.Key.ContainerNumber,
                        WarehouseId = cg.Key.WarehouseId,
                        WarehouseName = cg.Key.WarehouseName,
                        RollCount = cg.Sum(x => x.RollCount),
                        TotalMeters = cg.Sum(x => x.TotalMeters),
                        AvailableMeters = cg.Sum(x => x.AvailableMeters),
                        ColorCount = cg.Select(x => x.FabricColorId).Distinct().Count()
                    })
                    .OrderByDescending(c => c.RollCount)
                    .ToList();

                return new FabricSearchInsight
                {
                    FabricItemId = g.Key.FabricItemId,
                    FabricCode = g.Key.FabricCode,
                    FabricName = g.Key.FabricName,
                    ContainerCount = containers.Count,
                    TotalRolls = g.Sum(x => x.RollCount),
                    TotalMeters = g.Sum(x => x.TotalMeters),
                    AvailableMeters = g.Sum(x => x.AvailableMeters),
                    Locations = containers
                };
            })
            .OrderByDescending(i => i.TotalRolls)
            .ToList();
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
                CreateMetricValueTextBlock(value, theme.Value),
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
            "إجمالي الأثواب",
            AppFormats.Number(stock.Sum(s => s.RollCount)),
            "\uE8CB",
            new("#F5F3FF", "#DDD6FE", "#7C3AED", "#5B21B6", "#6D28D9")));
        panel.Children.Add(CreateMetricCard(
            "الأمتار",
            AppFormats.Number(stock.Sum(s => s.TotalMeters), 0),
            "\uE81E",
            new("#EFF6FF", "#BFDBFE", "#2563EB", "#1D4ED8", "#1E40AF")));
        panel.Children.Add(CreateMetricCard(
            "اليارد",
            AppFormats.Number(stock.Sum(s => s.TotalYards), 0),
            "\uE81E",
            new("#ECFEFF", "#A5F3FC", "#0891B2", "#0E7490", "#155E75")));
        panel.Children.Add(CreateMetricCard(
            "المتاح",
            AppFormats.Number(stock.Sum(s => s.AvailableMeters), 0),
            "\uE73E",
            new("#ECFDF5", "#A7F3D0", "#059669", "#047857", "#065F46")));
        panel.Children.Add(CreateMetricCard(
            "قيمة المخزون",
            AppFormats.CurrencyUsd(stock.Sum(s => s.InventoryValue)),
            "\uE8C1",
            new("#FFFBEB", "#FDE68A", "#D97706", "#B45309", "#92400E")));
    }

    private static TextBlock CreateMetricValueTextBlock(string value, string themeValueHex)
    {
        var textBlock = new TextBlock
        {
            Text = value,
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Foreground = HexBrush(themeValueHex),
            Margin = new Thickness(0, 8, 0, 4),
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            Language = XmlLanguage.GetLanguage("en-US")
        };
        return textBlock;
    }

    public static TextBox CreateSearchBox(double width = 320) => new()
    {
        Width = width,
        Height = 36,
        VerticalContentAlignment = VerticalAlignment.Center,
        FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
        FontSize = 13,
        Padding = new Thickness(10, 0, 10, 0),
        ToolTip = "ابحث باسم التوب أو الكود أو اللون أو رقم الحاوية"
    };

    public static Border CreateInsightHost() => new()
    {
        Visibility = Visibility.Collapsed,
        Margin = new Thickness(0, 8, 0, 8),
        Padding = new Thickness(14, 12, 14, 12),
        CornerRadius = new CornerRadius(10),
        BorderThickness = new Thickness(1),
        Background = HexBrush("#EFF6FF"),
        BorderBrush = HexBrush("#BFDBFE")
    };

    public static void WireSearchDebounced(
        TextBox searchBox,
        DispatcherTimer timer,
        Action<string> onSearch)
    {
        var pending = "";
        searchBox.TextChanged += (_, _) =>
        {
            pending = searchBox.Text ?? "";
            timer.Stop();
            timer.Start();
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            onSearch(pending);
        };
    }

    public static void RenderSearchInsightPanel(
        Border insightHost,
        IReadOnlyList<FabricStockBalanceDto> stock,
        string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || stock.Count == 0)
        {
            insightHost.Visibility = Visibility.Collapsed;
            insightHost.Child = null;
            return;
        }

        var insights = BuildSearchInsights(stock);
        if (insights.Count == 0)
        {
            insightHost.Visibility = Visibility.Collapsed;
            insightHost.Child = null;
            return;
        }

        var root = new StackPanel();
        root.Children.Add(new TextBlock
        {
            Text = $"نتائج البحث عن «{searchTerm}» — {insights.Count} توب في {stock.Select(s => s.ContainerId).Distinct().Count()} حاوية",
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Foreground = HexBrush("#1E3A8A"),
            Margin = new Thickness(0, 0, 0, 8)
        });

        foreach (var insight in insights.Take(8))
        {
            var card = new Border
            {
                Background = Brushes.White,
                BorderBrush = HexBrush("#DBEAFE"),
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
                Foreground = HexBrush("#0F172A")
            });
            body.Children.Add(new TextBlock
            {
                Text = $"موجود في {insight.ContainerCount} حاوية • {AppFormats.Number(insight.TotalRolls)} ثوب • {AppFormats.Number(insight.TotalMeters, 0)} م • متاح {AppFormats.Number(insight.AvailableMeters, 0)} م",
                FontSize = 11,
                Foreground = HexBrush("#475569"),
                Margin = new Thickness(0, 4, 0, 8)
            });

            foreach (var loc in insight.Locations.Take(12))
            {
                body.Children.Add(new TextBlock
                {
                    Text = $"• حاوية {loc.ContainerNumber} — {loc.WarehouseName} — {AppFormats.Number(loc.RollCount)} ثوب — {AppFormats.Number(loc.TotalMeters, 0)} م" +
                           (loc.ColorCount > 1 ? $" — {loc.ColorCount} ألوان" : ""),
                    FontSize = 12,
                    Foreground = HexBrush("#1E293B"),
                    Margin = new Thickness(4, 0, 0, 2)
                });
            }

            if (insight.Locations.Count > 12)
            {
                body.Children.Add(new TextBlock
                {
                    Text = $"… و {insight.Locations.Count - 12} حاوية إضافية في الجدول أدناه",
                    FontSize = 11,
                    Foreground = HexBrush("#64748B"),
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
                Foreground = HexBrush("#64748B")
            });
        }

        insightHost.Child = root;
        insightHost.Visibility = Visibility.Visible;
    }

    public static StackPanel CreateFilterRow(
        string warehouseLabel,
        ComboBox warehouseCombo,
        string containerLabel,
        ComboBox containerCombo,
        UIElement? searchBox = null)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 4)
        };

        if (searchBox is not null)
        {
            row.Children.Add(CreateFilterLabel("بحث ذكي:"));
            row.Children.Add(searchBox);
            row.Children.Add(CreateFilterLabel(warehouseLabel, new Thickness(16, 0, 10, 0)));
        }
        else
        {
            row.Children.Add(CreateFilterLabel(warehouseLabel));
        }

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

internal sealed class FabricSearchInsight
{
    public Guid FabricItemId { get; init; }
    public string FabricCode { get; init; } = "";
    public string FabricName { get; init; } = "";
    public int ContainerCount { get; init; }
    public int TotalRolls { get; init; }
    public decimal TotalMeters { get; init; }
    public decimal AvailableMeters { get; init; }
    public IReadOnlyList<FabricContainerLocation> Locations { get; init; } = [];
}

internal sealed class FabricContainerLocation
{
    public Guid ContainerId { get; init; }
    public string ContainerNumber { get; init; } = "";
    public Guid WarehouseId { get; init; }
    public string WarehouseName { get; init; } = "";
    public int RollCount { get; init; }
    public decimal TotalMeters { get; init; }
    public decimal AvailableMeters { get; init; }
    public int ColorCount { get; init; }
}

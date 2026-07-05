using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Dialogs;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ERPSystem.Controls.Inventory;

internal static class WarehouseExecutiveWorkspaceBuilder
{
    public static UIElement Build(
        InventoryOperationsCenterDto oc,
        Action<string>? navigateTab = null,
        Action? openActionPanel = null)
    {
        var ex = oc.Executive;
        var w = oc.Warehouse;
        var accent = Br("AccentInventoryBrush") as SolidColorBrush ?? new SolidColorBrush(Colors.Teal);

        var root = new StackPanel { MaxWidth = 1400 };

        root.Children.Add(BuildExecutiveHeader(w, oc.CostCenterName, openActionPanel));
        root.Children.Add(BuildQuantityTiles(ex.Quantities, accent, navigateTab));
        root.Children.Add(BuildMainGrid(ex, w, accent, navigateTab));

        if (ex.Activity30Days.Count > 0)
            root.Children.Add(BuildActivityChart(ex.Activity30Days, accent));

        var detailTabs = BuildDetailTabs(oc, navigateTab);
        root.Children.Add(detailTabs);

        ApplyFadeIn(root);
        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = ErpDesignTokens.PagePadding,
            Content = root,
            Background = Br("AppBgBrush") as SolidColorBrush
        };
    }

    private static UIElement BuildExecutiveHeader(
        WarehouseListExtendedDto w,
        string? costCenter,
        Action? openActionPanel)
    {
        var card = ErpUiFactory.Card(new Grid(), new Thickness(0, 0, 0, ErpDesignTokens.SpaceMd));
        card.Padding = new Thickness(0);
        if (card.Child is not Grid grid) return card;

        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        grid.Children.Add(new Border
        {
            Height = 4,
            CornerRadius = new CornerRadius(ErpDesignTokens.CardRadius, ErpDesignTokens.CardRadius, 0, 0),
            Background = Br("AccentInventoryBrush")
        });

        var body = new Grid { Margin = ErpDesignTokens.CardPadding };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var left = new StackPanel();
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        titleRow.Children.Add(new TextBlock
        {
            Text = w.NameAr,
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Foreground = Br("TextPrimaryBrush"),
            VerticalAlignment = VerticalAlignment.Center
        });
        titleRow.Children.Add(new Border
        {
            Background = w.IsActive ? Br("SuccessBgBrush") : Br("DangerBgBrush"),
            CornerRadius = new CornerRadius(100),
            Padding = new Thickness(12, 4, 12, 4),
            Margin = new Thickness(16, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = w.IsActive ? "نشط" : "معطل",
                FontWeight = FontWeights.SemiBold,
                Foreground = w.IsActive ? Br("SuccessBrush") : Br("DangerBrush")
            }
        });
        left.Children.Add(titleRow);
        left.Children.Add(new TextBlock
        {
            Text = $"كود: {w.Code}  •  {w.City}" +
                   (string.IsNullOrWhiteSpace(w.Manager) ? "" : $"  •  المدير: {w.Manager}") +
                   (string.IsNullOrWhiteSpace(costCenter) ? "" : $"  •  مركز التكلفة: {costCenter}"),
            Margin = new Thickness(0, 8, 0, 0),
            Foreground = Br("TextSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap
        });

        Grid.SetColumn(left, 0);
        body.Children.Add(left);

        var actionsBtn = new Button
        {
            Content = "إجراءات سريعة \uE712",
            Padding = new Thickness(16, 10, 16, 10),
            Cursor = Cursors.Hand,
            FontFamily = new FontFamily("Segoe UI, Segoe MDL2 Assets"),
            Style = (Style)System.Windows.Application.Current.Resources["PrimaryButtonStyle"]!
        };
        actionsBtn.Click += (_, _) => openActionPanel?.Invoke();
        Grid.SetColumn(actionsBtn, 1);
        body.Children.Add(actionsBtn);

        Grid.SetRow(body, 1);
        grid.Children.Add(body);
        return card;
    }

    private static UIElement BuildQuantityTiles(
        WarehouseQuantityMetricsDto q,
        SolidColorBrush accent,
        Action<string>? navigateTab)
    {
        var wrap = new WrapPanel { Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceMd) };
        foreach (var (title, value, icon, desc, tab) in new (string, string, string, string, string?)[]
        {
            ("Rolls", q.TotalRolls.ToString("N0"), "\uE8B7", "إجمالي", "Rolls"),
            ("أمتار", $"{q.TotalMeters:N1}", "\uE8CB", "إجمالي", "Stock"),
            ("متاح", $"{q.AvailableMeters:N1}", "\uE73E", "م", "Stock"),
            ("محجوز", $"{q.ReservedMeters:N1}", "\uE8F1", "م", "Stock"),
            ("تالف", $"{q.DamagedMeters:N1}", "\uE783", "م", "Rolls"),
            ("محظور", $"{q.BlockedMeters:N1}", "\uE72E", "م", "Rolls")
        })
        {
            var card = new MetricCardControl
            {
                CardTitle = title,
                CardValue = value,
                CardIcon = icon,
                CardDescription = desc,
                AccentColor = accent,
                MinWidth = 150,
                Margin = new Thickness(0, 0, ErpDesignTokens.CardGap, ErpDesignTokens.CardGap),
                Cursor = Cursors.Hand
            };
            if (!string.IsNullOrEmpty(tab))
            {
                var captured = tab!;
                card.MouseLeftButtonUp += (_, _) => navigateTab?.Invoke(captured);
            }
            wrap.Children.Add(card);
        }
        return wrap;
    }

    private static UIElement BuildMainGrid(
        WarehouseExecutiveDashboardDto ex,
        WarehouseListExtendedDto w,
        SolidColorBrush accent,
        Action<string>? navigateTab)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, ErpDesignTokens.SpaceMd) };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());

        var valueCard = BuildValueCard(ex, accent);
        Grid.SetColumn(valueCard, 0);
        Grid.SetRow(valueCard, 0);
        grid.Children.Add(valueCard);

        var lastTx = ex.LastTransaction is not null
            ? BuildLastTransactionCard(ex.LastTransaction)
            : EmptyPanel("لا توجد حركات بعد", "ستظهر آخر معاملة عند ترحيل مخزون أو مناقلة.");
        Grid.SetColumn(lastTx, 1);
        Grid.SetRow(lastTx, 0);
        grid.Children.Add(lastTx);

        var movements = ex.RecentMovements.Count > 0
            ? BuildMovementsPanel(ex.RecentMovements)
            : EmptyPanel("لا حركات حديثة", "الحركات المسجلة في PostgreSQL ستظهر هنا.");
        Grid.SetColumn(movements, 0);
        Grid.SetRow(movements, 1);
        grid.Children.Add(movements);

        var rightCol = new StackPanel();
        if (ex.TopMovingFabrics.Count > 0)
            rightCol.Children.Add(BuildTopFabricsCard(ex.TopMovingFabrics, accent));
        if (ex.Alerts.Count > 0)
            rightCol.Children.Add(BuildAlertsPanel(ex.Alerts, navigateTab));
        else
            rightCol.Children.Add(EmptyPanel("لا تنبيهات", "المستودع في حالة طبيعية."));
        if (ex.RecentDocuments.Count > 0)
            rightCol.Children.Add(BuildDocumentsPanel(ex.RecentDocuments, w.Id));
        if (ex.LastUserActivity is not null)
            rightCol.Children.Add(BuildUserActivityCard(ex.LastUserActivity));

        if (rightCol.Children.Count == 0)
            rightCol.Children.Add(EmptyPanel("—", "لا بيانات إضافية"));

        Grid.SetColumn(rightCol, 1);
        Grid.SetRow(rightCol, 1);
        grid.Children.Add(rightCol);

        return grid;
    }

    private static Border BuildValueCard(WarehouseExecutiveDashboardDto ex, SolidColorBrush accent)
    {
        var sp = new StackPanel();
        var trend = ex.ValueTrendPercent30d;
        sp.Children.Add(new MetricCardControl
        {
            CardTitle = "قيمة المخزون",
            CardValue = $"${ex.TotalInventoryValue:N0}",
            CardIcon = "\uE8C1",
            TrendValue = trend != 0 ? $"{(trend > 0 ? "+" : "")}{trend:N1}% / 30 يوم" : "",
            TrendDirection = trend > 0 ? MetricTrend.Up : trend < 0 ? MetricTrend.Down : MetricTrend.Neutral,
            AccentColor = accent,
            Margin = new Thickness(0, 0, 0, 12)
        });

        if (ex.ValueSparkline30d.Count > 1)
            sp.Children.Add(BuildSparkline(ex.ValueSparkline30d, accent));

        if (ex.ValueByFabric.Count > 0)
        {
            sp.Children.Add(SectionLabel("حسب نوع القماش"));
            sp.Children.Add(BuildSliceBars(ex.ValueByFabric, accent));
        }
        if (ex.ValueByCategory.Count > 0)
        {
            sp.Children.Add(SectionLabel("حسب التصنيف"));
            sp.Children.Add(BuildSliceBars(ex.ValueByCategory, Br("PrimaryBrush") as SolidColorBrush ?? accent));
        }

        sp.Children.Add(new TextBlock
        {
            Text = "USD",
            FontSize = 11,
            Foreground = Br("TextMutedBrush"),
            Margin = new Thickness(0, 8, 0, 0)
        });

        var card = PanelCard("تقييم المخزون", sp, margin: new Thickness(0, 0, 8, 8));
        card.Cursor = Cursors.Hand;
        card.MouseLeftButtonUp += (_, _) => ShowValueDrillDown(ex);
        return card;
    }

    private static void ShowValueDrillDown(WarehouseExecutiveDashboardDto ex)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = $"إجمالي: ${ex.TotalInventoryValue:N2}",
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 12)
        });
        if (ex.ValueByFabric.Count > 0)
        {
            sp.Children.Add(SectionLabel("حسب القماش"));
            var g = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, MaxHeight = 200, ItemsSource = ex.ValueByFabric };
            ErpUiFactory.AddGridColumn(g, "النوع", nameof(WarehouseValueSliceDto.Label), "*", null);
            ErpUiFactory.AddGridColumn(g, "القيمة", nameof(WarehouseValueSliceDto.Value), 100, "N2");
            ErpUiFactory.AddGridColumn(g, "%", nameof(WarehouseValueSliceDto.Percent), 60, "N1");
            sp.Children.Add(g);
        }
        if (ex.ValueByCategory.Count > 0)
        {
            sp.Children.Add(SectionLabel("حسب التصنيف"));
            var g2 = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, MaxHeight = 160, ItemsSource = ex.ValueByCategory };
            ErpUiFactory.AddGridColumn(g2, "التصنيف", nameof(WarehouseValueSliceDto.Label), "*", null);
            ErpUiFactory.AddGridColumn(g2, "القيمة", nameof(WarehouseValueSliceDto.Value), 100, "N2");
            sp.Children.Add(g2);
        }
        ErpModalWindow.Show("تفصيل قيمة المخزون", "PostgreSQL — live", sp, "\uE8C1", 520, 560);
    }

    private static Border BuildLastTransactionCard(WarehouseMovementCardDto m)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = m.Type,
            FontWeight = FontWeights.SemiBold,
            FontSize = 16,
            Foreground = Br("TextPrimaryBrush")
        });
        sp.Children.Add(InfoLine($"{m.FromLabel}  →  {m.ToLabel}"));
        sp.Children.Add(InfoLine($"{m.QuantityMeters:N2} م  •  ${m.TotalValue:N2}"));
        sp.Children.Add(InfoLine($"مرجع: {m.ReferenceNumber ?? m.MovementNumber}"));
        sp.Children.Add(InfoLine($"{m.Username}  •  {m.Timestamp:yyyy/MM/dd HH:mm}"));
        var card = PanelCard("آخر معاملة", sp, margin: new Thickness(8, 0, 0, 8));
        card.Cursor = Cursors.Hand;
        card.MouseLeftButtonUp += (_, _) => InventoryPopupService.ShowMovementDetail(m);
        return card;
    }

    private static Border BuildMovementsPanel(IReadOnlyList<WarehouseMovementCardDto> movements)
    {
        var sp = new StackPanel();
        foreach (var m in movements)
        {
            var row = new Border
            {
                Background = Br("SurfaceAltBrush"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 6),
                Cursor = Cursors.Hand
            };
            row.MouseEnter += (_, _) => row.Background = Br("PrimaryVeryLightBrush");
            row.MouseLeave += (_, _) => row.Background = Br("SurfaceAltBrush");
            row.MouseLeftButtonUp += (_, _) => InventoryPopupService.ShowMovementDetail(m);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            grid.Children.Add(new TextBlock
            {
                Text = m.TypeIcon,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Foreground = Br("AccentInventoryBrush")
            });

            var mid = new StackPanel();
            mid.Children.Add(new TextBlock
            {
                Text = $"{m.Type} — {m.MovementNumber}",
                FontWeight = FontWeights.SemiBold,
                Foreground = Br("TextPrimaryBrush")
            });
            mid.Children.Add(new TextBlock
            {
                Text = $"{m.FromLabel} → {m.ToLabel}  •  {m.QuantityMeters:N1} م",
                FontSize = 12,
                Foreground = Br("TextSecondaryBrush"),
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(mid, 1);
            grid.Children.Add(mid);

            var right = new TextBlock
            {
                Text = $"{m.Timestamp:MM/dd HH:mm}\n{m.Username}",
                FontSize = 11,
                TextAlignment = TextAlignment.Left,
                Foreground = Br("TextMutedBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(right, 2);
            grid.Children.Add(right);

            row.Child = grid;
            sp.Children.Add(row);
        }
        return PanelCard("آخر 5 حركات", sp, margin: new Thickness(0, 8, 8, 0));
    }

    private static Border BuildTopFabricsCard(IReadOnlyList<WarehouseTopFabricDto> items, SolidColorBrush accent)
    {
        var sp = new StackPanel();
        var max = items.Max(x => x.MetersMoved);
        foreach (var item in items)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            row.Children.Add(new TextBlock
            {
                Text = item.FabricName,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            });
            row.Children.Add(new TextBlock
            {
                Text = $"{item.MetersMoved:N0} م",
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = Br("TextSecondaryBrush"),
                FontSize = 12
            });
            Grid.SetColumn(row.Children[1], 1);
            sp.Children.Add(row);
            sp.Children.Add(MiniBar(max > 0 ? (double)(item.MetersMoved / max) : 0, accent));
        }
        return PanelCard("أكثر الأقمشة حركة (30 يوم)", sp, margin: new Thickness(8, 8, 0, 8));
    }

    private static Border BuildAlertsPanel(IReadOnlyList<WarehouseAlertCardDto> alerts, Action<string>? navigateTab)
    {
        var sp = new StackPanel();
        foreach (var a in alerts)
        {
            var (bg, fg) = a.Severity switch
            {
                "Critical" => (Br("DangerBgBrush"), Br("DangerBrush")),
                "Warning" => (Br("WarningBgBrush"), Br("WarningBrush")),
                _ => (Br("InfoBgBrush"), Br("InfoBrush"))
            };
            var row = new Border
            {
                Background = bg,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 6),
                Cursor = Cursors.Hand
            };
            row.Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = a.Title, FontWeight = FontWeights.SemiBold, Foreground = fg },
                    new TextBlock { Text = a.Message, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) }
                }
            };
            row.MouseLeftButtonUp += (_, _) =>
            {
                if (a.NavigationTarget == "Transfers")
                    InventoryPopupService.ShowTransferWizard();
                else if (a.NavigationTarget == "Stocktake")
                    InventoryPopupService.ShowStocktakeWizard();
                else if (a.DocumentId.HasValue && a.AlertType == "PendingTransfer")
                    InventoryPopupService.ShowTransferWizard(transferId: a.DocumentId);
                else if (!string.IsNullOrEmpty(a.NavigationTarget))
                    navigateTab?.Invoke(a.NavigationTarget);
            };
            sp.Children.Add(row);
        }
        return PanelCard("تنبيهات", sp, margin: new Thickness(8, 0, 0, 8));
    }

    private static Border BuildDocumentsPanel(IReadOnlyList<WarehouseDocumentCardDto> docs, Guid warehouseId)
    {
        var sp = new StackPanel();
        foreach (var d in docs)
        {
            var row = new Border
            {
                Padding = new Thickness(0, 6, 0, 6),
                BorderBrush = Br("BorderBrush"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Cursor = Cursors.Hand
            };
            row.Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = $"{DocLabel(d.DocumentType)} — {d.Number}", FontWeight = FontWeights.SemiBold },
                    new TextBlock { Text = $"{d.Status}  •  {d.Date:yyyy/MM/dd}", FontSize = 11, Foreground = Br("TextMutedBrush") }
                }
            };
            row.MouseLeftButtonUp += (_, _) => OpenDocument(d, warehouseId);
            sp.Children.Add(row);
        }
        return PanelCard("آخر المستندات", sp, margin: new Thickness(8, 0, 0, 0));
    }

    private static Border BuildUserActivityCard(WarehouseUserActivityDto a)
    {
        var sp = new StackPanel();
        sp.Children.Add(InfoLine($"المستخدم: {a.Username}"));
        sp.Children.Add(InfoLine($"الإجراء: {a.ActionType}"));
        sp.Children.Add(InfoLine($"الوقت: {a.Timestamp:yyyy/MM/dd HH:mm}"));
        return PanelCard("آخر نشاط مستخدم", sp, margin: new Thickness(8, 8, 0, 0));
    }

    private static UIElement BuildActivityChart(IReadOnlyList<WarehouseDailyActivityDto> days, SolidColorBrush accent)
    {
        var canvas = new Canvas { Height = 140, Margin = new Thickness(0, 8, 0, 0) };
        var max = days.Max(d => Math.Max(d.IncomingMeters, d.OutgoingMeters));
        if (max <= 0) max = 1;
        var w = Math.Max(600, days.Count * 18);
        canvas.Width = w;

        for (var i = 0; i < days.Count; i++)
        {
            var d = days[i];
            var x = i * 18 + 4;
            var inH = (double)(d.IncomingMeters / max) * 100;
            var outH = (double)(d.OutgoingMeters / max) * 100;
            canvas.Children.Add(new Rectangle
            {
                Width = 6, Height = Math.Max(1, inH),
                Fill = Br("SuccessBrush"),
                RadiusX = 2, RadiusY = 2
            });
            Canvas.SetLeft(canvas.Children[^1], x);
            Canvas.SetBottom(canvas.Children[^1], 20);
            canvas.Children.Add(new Rectangle
            {
                Width = 6, Height = Math.Max(1, outH),
                Fill = Br("DangerBrush"),
                RadiusX = 2, RadiusY = 2
            });
            Canvas.SetLeft(canvas.Children[^1], x + 8);
            Canvas.SetBottom(canvas.Children[^1], 20);
        }

        var legend = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        legend.Children.Add(LegendDot("وارد", Br("SuccessBrush")));
        legend.Children.Add(LegendDot("صادر", Br("DangerBrush")));
        var net30 = days.Sum(d => d.NetMeters);
        legend.Children.Add(new TextBlock
        {
            Text = $"صافي 30 يوم: {net30:N1} م",
            Margin = new Thickness(16, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Br("TextSecondaryBrush")
        });

        var sp = new StackPanel();
        sp.Children.Add(canvas);
        sp.Children.Add(legend);
        return PanelCard("نشاط المستودع — 30 يوم", sp, margin: new Thickness(0, 0, 0, ErpDesignTokens.SpaceMd));
    }

    private static TabControl BuildDetailTabs(InventoryOperationsCenterDto oc, Action<string>? navigateTab)
    {
        var tabs = new TabControl { FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"), Margin = new Thickness(0, 8, 0, 0) };
        tabs.Items.Add(Tab("Stock", "الأرصدة", DataTab(oc.Stock,
            ("القماش", nameof(FabricStockBalanceDto.FabricName), null),
            ("اللون", nameof(FabricStockBalanceDto.ColorName), null),
            ("Rolls", nameof(FabricStockBalanceDto.RollCount), null),
            ("متاح", nameof(FabricStockBalanceDto.AvailableMeters), "N2"),
            ("قيمة", nameof(FabricStockBalanceDto.InventoryValue), "N2"))));
        tabs.Items.Add(Tab("Rolls", "Rolls", DataTab(oc.Rolls,
            ("#", nameof(FabricRollListDto.RollNumber), null),
            ("القماش", nameof(FabricRollListDto.FabricName), null),
            ("متبقي", nameof(FabricRollListDto.RemainingLengthMeters), "N2"),
            ("قيمة", nameof(FabricRollListDto.CurrentValue), "N2"),
            ("حالة", nameof(FabricRollListDto.Status), null))));
        if (oc.Locations.Count > 0)
            tabs.Items.Add(Tab("Locations", "المواقع", DataTab(oc.Locations,
                ("كود", nameof(StorageLocationDto.Code), null),
                ("اسم", nameof(StorageLocationDto.Name), null),
                ("نوع", nameof(StorageLocationDto.LocationType), null),
                ("حالة", nameof(StorageLocationDto.Status), null))));
        tabs.Items.Add(Tab("Movements", "كل الحركات", DataTab(oc.RecentMovements,
            ("رقم", nameof(StockMovementListDto.MovementNumber), null),
            ("نوع", nameof(StockMovementListDto.Type), null),
            ("أمتار", nameof(StockMovementListDto.TotalMeters), "N2"),
            ("قيمة", nameof(StockMovementListDto.TotalValue), "N2"))));
        if (oc.RecentAudit.Count > 0)
            tabs.Items.Add(Tab("Audit", "التدقيق", DataTab(oc.RecentAudit,
                ("تاريخ", nameof(InventoryAuditDto.RecordedAt), null),
                ("إجراء", nameof(InventoryAuditDto.Action), null),
                ("مستخدم", nameof(InventoryAuditDto.Username), null))));
        if (oc.Timeline.Count > 0)
            tabs.Items.Add(Tab("Timeline", "الخط الزمني", DataTab(oc.Timeline,
                ("تاريخ", nameof(InventoryTimelineDto.OccurredAt), null),
                ("حدث", nameof(InventoryTimelineDto.Title), null),
                ("مستخدم", nameof(InventoryTimelineDto.Username), null))));
        return tabs;
    }

    private static void OpenDocument(WarehouseDocumentCardDto d, Guid warehouseId)
    {
        switch (d.NavigationTarget)
        {
            case "TransferWizard":
                InventoryPopupService.ShowTransferWizard(transferId: d.Id);
                break;
            case "StocktakeWizard":
                InventoryPopupService.ShowStocktakeWizard(sessionId: d.Id);
                break;
            case "OpeningStockForm":
                InventoryNavigationContext.BeginCreateOpeningStock(warehouseId);
                InventoryPopupService.ShowOpeningStockForm(warehouseId);
                break;
        }
    }

    private static string DocLabel(string t) => t switch
    {
        "Transfer" => "مناقلة",
        "Stocktake" => "جرد",
        "OpeningStock" => "أول المدة",
        _ => t
    };

    private static TabItem Tab(string key, string label, UIElement content) =>
        new() { Header = label, Tag = key, Content = new ScrollViewer { Content = content, Padding = new Thickness(12) } };

    private static UIElement DataTab<T>(IReadOnlyList<T> data, params (string Header, string Path, string? Format)[] cols)
    {
        if (data.Count == 0)
            return EmptyPanel("لا بيانات", "لا توجد سجلات في PostgreSQL لهذا القسم.");
        var g = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, ItemsSource = data, MaxHeight = 400 };
        foreach (var (h, p, fmt) in cols)
            ErpUiFactory.AddGridColumn(g, h, p, "*", fmt);
        return g;
    }

    private static Border PanelCard(string title, UIElement body, Thickness? margin = null) => new()
    {
        Background = Br("SurfaceBrush"),
        BorderBrush = Br("BorderBrush"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(ErpDesignTokens.CardRadius),
        Padding = new Thickness(16),
        Margin = margin ?? new Thickness(0),
        Child = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 14,
                    Foreground = Br("TextPrimaryBrush"),
                    Margin = new Thickness(0, 0, 0, 12)
                },
                body
            }
        }
    };

    private static Border EmptyPanel(string title, string hint) => PanelCard(title, new TextBlock
    {
        Text = hint,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Br("TextMutedBrush"),
        FontSize = 13
    });

    private static TextBlock SectionLabel(string t) => new()
    {
        Text = t,
        FontSize = 11,
        FontWeight = FontWeights.SemiBold,
        Foreground = Br("TextMutedBrush"),
        Margin = new Thickness(0, 8, 0, 4)
    };

    private static TextBlock InfoLine(string t) => new()
    {
        Text = t,
        Margin = new Thickness(0, 4, 0, 0),
        Foreground = Br("TextSecondaryBrush"),
        TextWrapping = TextWrapping.Wrap
    };

    private static UIElement BuildSparkline(IReadOnlyList<decimal> points, SolidColorBrush accent)
    {
        var canvas = new Canvas { Height = 40, Margin = new Thickness(0, 4, 0, 0) };
        if (points.Count < 2) return canvas;
        var max = points.Max();
        var min = points.Min();
        var range = max - min;
        if (range == 0) range = 1;
        var poly = new Polyline { Stroke = accent, StrokeThickness = 2, Fill = Brushes.Transparent };
        for (var i = 0; i < points.Count; i++)
        {
            var x = i * (280.0 / (points.Count - 1));
            var y = 36 - (double)((points[i] - min) / range) * 32;
            poly.Points.Add(new Point(x, y));
        }
        canvas.Children.Add(poly);
        canvas.Width = 280;
        return canvas;
    }

    private static UIElement BuildSliceBars(IReadOnlyList<WarehouseValueSliceDto> slices, SolidColorBrush accent)
    {
        var sp = new StackPanel();
        foreach (var s in slices.Take(5))
        {
            sp.Children.Add(new Grid
            {
                Margin = new Thickness(0, 0, 0, 4),
                Children =
                {
                    new TextBlock { Text = $"{s.Label} ({s.Percent:N0}%)", FontSize = 11 },
                }
            });
            sp.Children.Add(MiniBar((double)(s.Percent / 100m), accent));
        }
        return sp;
    }

    private static UIElement MiniBar(double ratio, SolidColorBrush fill)
    {
        var track = new Grid { Height = 6, Margin = new Thickness(0, 0, 0, 6) };
        track.Children.Add(new Border { Background = Br("BorderLightBrush"), CornerRadius = new CornerRadius(3) });
        track.Children.Add(new Border
        {
            Background = fill,
            CornerRadius = new CornerRadius(3),
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = Math.Max(4, ratio * 200)
        });
        return track;
    }

    private static UIElement LegendDot(string label, Brush color)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 12, 0) };
        sp.Children.Add(new Border { Width = 10, Height = 10, Background = color, CornerRadius = new CornerRadius(2), Margin = new Thickness(0, 0, 6, 0) });
        sp.Children.Add(new TextBlock { Text = label, FontSize = 11, VerticalAlignment = VerticalAlignment.Center });
        return sp;
    }

    private static void ApplyFadeIn(UIElement element)
    {
        if (element is not FrameworkElement fe) return;
        fe.Opacity = 0;
        fe.Loaded += (_, _) =>
        {
            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            fe.BeginAnimation(UIElement.OpacityProperty, anim);
        };
    }

    private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
}

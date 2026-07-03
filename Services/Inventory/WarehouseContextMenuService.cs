using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ERPSystem.Services.Inventory;

/// <summary>
/// لوحة إجراءات المستودع — popup غني بالمعلومات والإجراءات.
/// </summary>
public static class WarehouseContextMenuService
{
    private static Popup? _popup;

    public static void Show(WarehouseListExtendedDto warehouse, UIElement placementTarget)
    {
        Close();

        var actions = EntityActionRegistry.GetActions(EntityType.Warehouse);
        if (actions.Count == 0) return;

        var panel = new Border
        {
            MinWidth = 320,
            MaxWidth = 380,
            Background = Br("SurfaceBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(0),
            Effect = new DropShadowEffect { BlurRadius = 24, ShadowDepth = 4, Opacity = 0.25, Color = Colors.Black }
        };

        var root = new StackPanel();
        root.Children.Add(BuildHeader(warehouse));
        root.Children.Add(new Border { Height = 1, Background = Br("BorderBrush"), Margin = new Thickness(12, 0, 12, 0) });

        var list = new StackPanel { Margin = new Thickness(8, 8, 8, 12) };
        string? currentGroup = null;

        foreach (var action in actions)
        {
            if (action.Id == EntityActionId.WarehouseActivate && warehouse.IsActive)
                continue;
            if (action.Id == EntityActionId.WarehouseArchive && !warehouse.IsActive)
                continue;

            if (action.GroupAr != currentGroup && action.GroupAr != null)
            {
                list.Children.Add(new TextBlock
                {
                    Text = action.GroupAr,
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Br("TextMutedBrush"),
                    Margin = new Thickness(8, 10, 8, 4)
                });
                currentGroup = action.GroupAr;
            }

            var captured = action;
            var btn = BuildActionButton(captured, warehouse);
            list.Children.Add(btn);
        }

        root.Children.Add(list);
        panel.Child = root;

        _popup = new Popup
        {
            Child = panel,
            PlacementTarget = placementTarget,
            Placement = PlacementMode.MousePoint,
            StaysOpen = false,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.Fade
        };
        _popup.Closed += (_, _) => _popup = null;
        _popup.IsOpen = true;
    }

    public static void Close()
    {
        if (_popup is null) return;
        _popup.IsOpen = false;
        _popup = null;
    }

    private static UIElement BuildHeader(WarehouseListExtendedDto w)
    {
        var sp = new StackPanel { Margin = new Thickness(16, 14, 16, 12) };

        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        titleRow.Children.Add(new Border
        {
            Width = 44, Height = 44, CornerRadius = new CornerRadius(10),
            Background = Br("SuccessBgBrush"), Margin = new Thickness(0, 0, 12, 0),
            Child = new TextBlock
            {
                Text = "\uE8B7", FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 18, Foreground = Br("AccentInventoryBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var titleStack = new StackPanel();
        titleStack.Children.Add(new TextBlock
        {
            Text = w.NameAr,
            FontWeight = FontWeights.SemiBold,
            FontSize = 15,
            Foreground = Br("TextPrimaryBrush")
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = $"{w.Code}  •  {(w.IsActive ? "نشط" : "معطل")}",
            FontSize = 11,
            Margin = new Thickness(0, 3, 0, 0),
            Foreground = Br("TextMutedBrush")
        });
        if (!string.IsNullOrWhiteSpace(w.Manager))
            titleStack.Children.Add(new TextBlock
            {
                Text = $"المدير: {w.Manager}",
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0),
                Foreground = Br("TextSecondaryBrush")
            });
        titleRow.Children.Add(titleStack);
        sp.Children.Add(titleRow);

        var kpiGrid = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        kpiGrid.ColumnDefinitions.Add(new ColumnDefinition());
        kpiGrid.ColumnDefinitions.Add(new ColumnDefinition());
        kpiGrid.RowDefinitions.Add(new RowDefinition());
        kpiGrid.RowDefinitions.Add(new RowDefinition());

        AddKpi(kpiGrid, "قيمة المخزون", $"${w.InventoryValue:N0}", 0, 0);
        AddKpi(kpiGrid, "Rolls", w.RollCount.ToString("N0"), 1, 0);
        AddKpi(kpiGrid, "الأمتار", $"{w.TotalMeters:N1} م", 0, 1);
        AddKpi(kpiGrid, "المدينة", w.City, 1, 1);
        sp.Children.Add(kpiGrid);

        return sp;
    }

    private static void AddKpi(Grid grid, string label, string value, int col, int row)
    {
        var box = new StackPanel { Margin = new Thickness(4) };
        box.Children.Add(new TextBlock { Text = label, FontSize = 10, Foreground = Br("TextMutedBrush") });
        box.Children.Add(new TextBlock { Text = value, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Br("TextPrimaryBrush") });
        Grid.SetColumn(box, col);
        Grid.SetRow(box, row);
        grid.Children.Add(box);
    }

    private static Button BuildActionButton(EntityActionDefinition action, WarehouseListExtendedDto warehouse)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock
        {
            Text = action.IconGlyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            Width = 22,
            Foreground = action.IsDestructive ? Br("DangerBrush") : Br("TextSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Center
        });
        sp.Children.Add(new TextBlock
        {
            Text = action.LabelAr,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = action.IsDestructive ? Br("DangerBrush") : Br("TextPrimaryBrush")
        });

        var btn = new Button
        {
            Content = sp,
            HorizontalContentAlignment = HorizontalAlignment.Right,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(10, 8, 10, 8),
            Cursor = Cursors.Hand,
            Tag = action
        };

        btn.Click += (_, _) =>
        {
            Close();
            var displayName = EntityDisplayNameResolver.Resolve(warehouse, EntityType.Warehouse);
            if (action.RequiresConfirmation &&
                !ConfirmationDialogService.ConfirmDangerous(action.LabelAr, displayName))
                return;

            InventoryActionRouter.Handle(action.Id, warehouse, AppModule.Inventory);
        };

        return btn;
    }

    private static Brush Br(string key) =>
        (Brush)System.Windows.Application.Current.Resources[key]!;
}

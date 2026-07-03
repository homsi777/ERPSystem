using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Services.Inventory;

/// <summary>
/// قائمة مهام المستودع — تظهر عند النقر بزر اليمين على صف المستودع.
/// </summary>
public static class WarehouseContextMenuService
{
    public static void Show(WarehouseListExtendedDto warehouse, UIElement placementTarget)
    {
        var actions = EntityActionRegistry.GetActions(EntityType.Warehouse);
        if (actions.Count == 0) return;

        var menu = new ContextMenu
        {
            FlowDirection = FlowDirection.RightToLeft,
            MinWidth = 260,
            Padding = new Thickness(4),
            Background = Br("SurfaceBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(1)
        };

        menu.Items.Add(BuildHeader(warehouse));
        menu.Items.Add(new Separator());

        string? currentGroup = null;
        foreach (var action in actions)
        {
            if (action.Id == EntityActionId.WarehouseActivate && warehouse.IsActive)
                continue;
            if (action.Id == EntityActionId.WarehouseArchive && !warehouse.IsActive)
                continue;

            if (action.GroupAr != currentGroup && action.GroupAr != null)
            {
                if (menu.Items.Count > 2)
                    menu.Items.Add(new Separator());
                menu.Items.Add(BuildGroupHeader(action.GroupAr));
                currentGroup = action.GroupAr;
            }
            else if (action.GroupAr == null && currentGroup != null)
            {
                menu.Items.Add(new Separator());
                currentGroup = null;
            }

            var captured = action;
            var mi = new MenuItem
            {
                Header = BuildMenuHeader(captured),
                Tag = captured,
                FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
                FontSize = 13,
                Padding = new Thickness(12, 8, 12, 8)
            };

            if (captured.IsDestructive)
                mi.Foreground = Br("DangerBrush");

            mi.Click += (_, _) =>
            {
                var displayName = EntityDisplayNameResolver.Resolve(warehouse, EntityType.Warehouse);
                if (captured.RequiresConfirmation &&
                    !ConfirmationDialogService.ConfirmDangerous(captured.LabelAr, displayName))
                    return;

                InventoryActionRouter.Handle(captured.Id, warehouse, AppModule.Inventory);
            };

            menu.Items.Add(mi);
        }

        menu.PlacementTarget = placementTarget;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    private static MenuItem BuildHeader(WarehouseListExtendedDto w)
    {
        var sp = new StackPanel();

        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
        titleRow.Children.Add(new Border
        {
            Width = 36, Height = 36, CornerRadius = new CornerRadius(8),
            Background = Br("SuccessBgBrush"), Margin = new Thickness(0, 0, 10, 0),
            Child = new TextBlock
            {
                Text = "\uE8B7", FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 16, Foreground = Br("AccentInventoryBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });
        var titleStack = new StackPanel();
        titleStack.Children.Add(new TextBlock
        {
            Text = w.NameAr,
            FontWeight = FontWeights.SemiBold,
            FontSize = 14,
            Foreground = Br("TextPrimaryBrush")
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = $"{w.Code} • {(w.IsActive ? "نشط" : "معطل")}",
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0),
            Foreground = Br("TextMutedBrush")
        });
        titleRow.Children.Add(titleStack);
        sp.Children.Add(titleRow);

        sp.Children.Add(new TextBlock
        {
            Text = $"قيمة المخزون: ${w.InventoryValue:N0}  •  Rolls: {w.RollCount:N0}  •  {w.TotalMeters:N1} م",
            FontSize = 11,
            Foreground = Br("TextSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap
        });

        return new MenuItem
        {
            Header = sp,
            IsEnabled = false,
            Padding = new Thickness(12, 10, 12, 10),
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial")
        };
    }

    private static MenuItem BuildGroupHeader(string group) => new()
    {
        Header = group,
        IsEnabled = false,
        FontWeight = FontWeights.SemiBold,
        FontSize = 11,
        Padding = new Thickness(12, 6, 12, 4),
        Foreground = Br("TextMutedBrush")
    };

    private static StackPanel BuildMenuHeader(EntityActionDefinition action)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock
        {
            Text = action.IconGlyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = action.IsDestructive ? Br("DangerBrush") : Br("TextSecondaryBrush")
        });
        sp.Children.Add(new TextBlock
        {
            Text = action.LabelAr,
            VerticalAlignment = VerticalAlignment.Center
        });
        return sp;
    }

    private static Brush Br(string key) =>
        (Brush)System.Windows.Application.Current.Resources[key]!;
}

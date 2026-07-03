using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ERPSystem.Services.Inventory;

/// <summary>
/// قائمة مهام المستودع — نفس فلسفة ExpenseContextMenuService (ContextMenu عند المؤشر).
/// </summary>
public static class WarehouseContextMenuService
{
    public static void Show(WarehouseListExtendedDto warehouse, UIElement placementTarget)
    {
        var actions = EntityActionRegistry.GetActions(EntityType.Warehouse);
        if (actions.Count == 0) return;

        var filtered = actions
            .Where(a => !(a.Id == EntityActionId.WarehouseActivate && warehouse.IsActive))
            .Where(a => !(a.Id == EntityActionId.WarehouseArchive && !warehouse.IsActive))
            .ToList();

        var menu = new ContextMenu
        {
            FlowDirection = FlowDirection.RightToLeft,
            MinWidth = 220,
            Padding = new Thickness(4),
            Background = Br("SurfaceBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(1)
        };

        menu.Items.Add(BuildWarehouseHeader(warehouse));
        menu.Items.Add(new Separator());

        string? currentGroup = null;
        foreach (var action in filtered)
        {
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
        menu.Placement = PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    private static MenuItem BuildWarehouseHeader(WarehouseListExtendedDto w)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = w.NameAr,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Foreground = Br("TextPrimaryBrush")
        });
        sp.Children.Add(new TextBlock
        {
            Text = $"{w.Code} • {(w.IsActive ? "نشط" : "معطل")} • {w.RollCount:N0} rolls",
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0),
            Foreground = Br("TextMutedBrush")
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

    private static object BuildMenuHeader(EntityActionDefinition action)
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

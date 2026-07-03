using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Domain.Enums;
using ERPSystem.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Services.Capital;

/// <summary>
/// قائمة مهام الشريك — تظهر عند النقر بزر اليمين على بطاقة الشريك.
/// </summary>
public static class CapitalPartnerContextMenuService
{
    public static void Show(CapitalPartnerListDto partner, UIElement placementTarget)
    {
        _ = ShowAsync(partner, placementTarget);
    }

    private static async Task ShowAsync(CapitalPartnerListDto partner, UIElement placementTarget)
    {
        var perms = await LoadPermissionsAsync();
        var actions = EntityActionRegistry.GetActions(EntityType.CapitalPartner);
        if (actions.Count == 0) return;

        var menu = new ContextMenu
        {
            FlowDirection = FlowDirection.RightToLeft,
            MinWidth = 220,
            Padding = new Thickness(4),
            Background = (Brush)System.Windows.Application.Current.Resources["SurfaceBrush"]!,
            BorderBrush = (Brush)System.Windows.Application.Current.Resources["BorderBrush"]!,
            BorderThickness = new Thickness(1)
        };

        menu.Items.Add(BuildPartnerHeader(partner));
        menu.Items.Add(new Separator());

        string? currentGroup = null;
        foreach (var action in actions)
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
            var enabled = IsActionEnabled(captured.Id, partner, perms);
            var mi = new MenuItem
            {
                Header = BuildMenuHeader(captured),
                Tag = captured,
                IsEnabled = enabled,
                FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
                FontSize = 13,
                Padding = new Thickness(12, 8, 12, 8)
            };

            if (captured.IsDestructive)
                mi.Foreground = (Brush)System.Windows.Application.Current.Resources["DangerBrush"]!;

            mi.Click += (_, _) =>
            {
                if (!enabled) return;

                var displayName = EntityDisplayNameResolver.Resolve(partner, EntityType.CapitalPartner);
                if (captured.RequiresConfirmation &&
                    !ConfirmationDialogService.ConfirmDangerous(captured.LabelAr, displayName))
                    return;

                CapitalPartnerPopupService.HandleAction(captured.Id, partner);
            };

            menu.Items.Add(mi);
        }

        menu.PlacementTarget = placementTarget;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    private static MenuItem BuildPartnerHeader(CapitalPartnerListDto partner)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = partner.FullName,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Foreground = (Brush)System.Windows.Application.Current.Resources["TextPrimaryBrush"]!
        });
        sp.Children.Add(new TextBlock
        {
            Text = $"{partner.Code} • {partner.StatusDisplay}",
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0),
            Foreground = (Brush)System.Windows.Application.Current.Resources["TextMutedBrush"]!
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
        Foreground = (Brush)System.Windows.Application.Current.Resources["TextMutedBrush"]!
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
            Foreground = action.IsDestructive
                ? (Brush)System.Windows.Application.Current.Resources["DangerBrush"]!
                : (Brush)System.Windows.Application.Current.Resources["TextSecondaryBrush"]!
        });
        sp.Children.Add(new TextBlock
        {
            Text = action.LabelAr,
            VerticalAlignment = VerticalAlignment.Center
        });
        return sp;
    }

    private static bool IsActionEnabled(EntityActionId id, CapitalPartnerListDto partner, PartnerMenuPermissions perms)
    {
        if (partner.Status == PartnerStatus.Archived)
        {
            return id is EntityActionId.CapitalPartnerOperationsCenter
                or EntityActionId.CapitalPartnerDetails
                or EntityActionId.CapitalPartnerLedger
                or EntityActionId.CapitalPartnerAuditHistory
                or EntityActionId.CapitalPartnerTimeline
                or EntityActionId.CapitalPartnerExportPdf
                or EntityActionId.CapitalPartnerExportExcel
                or EntityActionId.CapitalPartnerPrint;
        }

        return id switch
        {
            EntityActionId.CapitalPartnerEdit => perms.CanEdit,
            EntityActionId.CapitalPartnerNewInvestment => perms.CanEdit,
            EntityActionId.CapitalPartnerWithdrawal => perms.CanEdit,
            EntityActionId.CapitalPartnerArchive => perms.CanArchive,
            _ => true
        };
    }

    private static async Task<PartnerMenuPermissions> LoadPermissionsAsync()
    {
        if (!AppServices.IsInitialized)
            return new PartnerMenuPermissions(false, false);

        var svc = CapitalPartnerUiService.Instance;
        var canEdit = await svc.CanEditAsync();
        var canArchive = await svc.CanArchiveAsync();
        return new PartnerMenuPermissions(canEdit, canArchive);
    }

    private sealed record PartnerMenuPermissions(bool CanEdit, bool CanArchive);
}

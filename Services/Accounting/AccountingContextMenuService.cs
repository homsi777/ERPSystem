using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Domain.Enums;
using ERPSystem.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Services.Accounting;

/// <summary>
/// قائمة مهام المحاسبة — تظهر عند النقر بزر اليمين على بطاقة حساب أو قيد.
/// </summary>
public static class AccountingContextMenuService
{
    public static void ShowAccount(AccountListDto account, UIElement placementTarget)
    {
        _ = ShowAccountAsync(account, placementTarget);
    }

    public static void ShowJournal(JournalEntryListDto entry, UIElement placementTarget)
    {
        _ = ShowJournalAsync(entry, placementTarget);
    }

    private static async Task ShowAccountAsync(AccountListDto account, UIElement placementTarget)
    {
        var perms = await LoadAccountPermissionsAsync();
        var actions = EntityActionRegistry.GetActions(EntityType.Account);
        if (actions.Count == 0) return;

        var menu = BuildMenuShell();
        menu.Items.Add(BuildAccountHeader(account));
        menu.Items.Add(new Separator());
        PopulateMenuItems(menu, actions, account, EntityType.Account, id => IsAccountActionEnabled(id, account, perms),
            id => AccountingPopupService.HandleAccountAction(id, account));
        menu.PlacementTarget = placementTarget;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    private static async Task ShowJournalAsync(JournalEntryListDto entry, UIElement placementTarget)
    {
        var perms = await LoadJournalPermissionsAsync();
        var actions = EntityActionRegistry.GetActions(EntityType.JournalEntry);
        if (actions.Count == 0) return;

        var menu = BuildMenuShell();
        menu.Items.Add(BuildJournalHeader(entry));
        menu.Items.Add(new Separator());
        PopulateMenuItems(menu, actions, entry, EntityType.JournalEntry, id => IsJournalActionEnabled(id, entry, perms),
            id => AccountingPopupService.HandleJournalAction(id, entry));
        menu.PlacementTarget = placementTarget;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    private static ContextMenu BuildMenuShell() => new()
    {
        FlowDirection = FlowDirection.RightToLeft,
        MinWidth = 220,
        Padding = new Thickness(4),
        Background = (Brush)System.Windows.Application.Current.Resources["SurfaceBrush"]!,
        BorderBrush = (Brush)System.Windows.Application.Current.Resources["BorderBrush"]!,
        BorderThickness = new Thickness(1)
    };

    private static void PopulateMenuItems(
        ContextMenu menu,
        IReadOnlyList<EntityActionDefinition> actions,
        object entity,
        EntityType entityType,
        Func<EntityActionId, bool> isEnabled,
        Func<EntityActionId, bool> onAction)
    {
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
            var enabled = isEnabled(captured.Id);
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

                var displayName = EntityDisplayNameResolver.Resolve(entity, entityType);
                if (captured.RequiresConfirmation &&
                    !ConfirmationDialogService.ConfirmDangerous(captured.LabelAr, displayName))
                    return;

                onAction(captured.Id);
            };

            menu.Items.Add(mi);
        }
    }

    private static MenuItem BuildAccountHeader(AccountListDto account)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = account.NameAr,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Foreground = (Brush)System.Windows.Application.Current.Resources["TextPrimaryBrush"]!
        });
        sp.Children.Add(new TextBlock
        {
            Text = $"{account.Code} • {account.AccountTypeDisplay} • {(account.IsPostable ? "قابل للترحيل" : "تجميعي")}",
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

    private static MenuItem BuildJournalHeader(JournalEntryListDto entry)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = entry.EntryNumber,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Foreground = (Brush)System.Windows.Application.Current.Resources["TextPrimaryBrush"]!
        });
        sp.Children.Add(new TextBlock
        {
            Text = $"{entry.EntryDate:yyyy/MM/dd} • {entry.StatusDisplay} • {entry.DebitTotal:N2} / {entry.CreditTotal:N2}",
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

    private static bool IsAccountActionEnabled(EntityActionId id, AccountListDto account, AccountMenuPermissions perms)
    {
        if (!account.IsActive)
        {
            return id is EntityActionId.AccountDetails;
        }

        return id switch
        {
            EntityActionId.AccountEdit => perms.CanEdit,
            EntityActionId.AccountAddChild => perms.CanCreate,
            EntityActionId.AccountCreate => perms.CanCreate,
            EntityActionId.AccountDeactivate => perms.CanEdit,
            _ => true
        };
    }

    private static bool IsJournalActionEnabled(EntityActionId id, JournalEntryListDto entry, JournalMenuPermissions perms)
    {
        if (entry.Status == JournalEntryStatus.Cancelled)
        {
            return id is EntityActionId.JournalView
                or EntityActionId.JournalDetails
                or EntityActionId.VoucherPrint
                or EntityActionId.VoucherExportPdf;
        }

        if (entry.Status == JournalEntryStatus.Reversed)
        {
            return id is EntityActionId.JournalView
                or EntityActionId.JournalDetails
                or EntityActionId.VoucherPrint
                or EntityActionId.VoucherExportPdf;
        }

        if (entry.Status == JournalEntryStatus.Posted)
        {
            return id switch
            {
                EntityActionId.JournalApprove => false,
                EntityActionId.JournalPost => false,
                EntityActionId.JournalCancel => false,
                EntityActionId.JournalReverse => perms.CanReverse,
                _ => true
            };
        }

        if (entry.Status == JournalEntryStatus.Approved)
        {
            return id switch
            {
                EntityActionId.JournalApprove => false,
                EntityActionId.JournalPost => perms.CanPost,
                EntityActionId.JournalCancel => perms.CanCreate,
                EntityActionId.JournalReverse => false,
                _ => true
            };
        }

        return id switch
        {
            EntityActionId.JournalApprove => perms.CanPost,
            EntityActionId.JournalPost => false,
            EntityActionId.JournalCancel => perms.CanCreate,
            EntityActionId.JournalReverse => false,
            _ => true
        };
    }

    private static async Task<AccountMenuPermissions> LoadAccountPermissionsAsync()
    {
        if (!AppServices.IsInitialized)
            return new AccountMenuPermissions(false, false);

        var svc = AccountingUiService.Instance;
        return new AccountMenuPermissions(
            await svc.CanEditAccountAsync(),
            await svc.CanCreateAccountAsync());
    }

    private static async Task<JournalMenuPermissions> LoadJournalPermissionsAsync()
    {
        if (!AppServices.IsInitialized)
            return new JournalMenuPermissions(false, false, false);

        var svc = AccountingUiService.Instance;
        return new JournalMenuPermissions(
            await svc.CanCreateJournalAsync(),
            await svc.CanPostJournalAsync(),
            await svc.CanReverseJournalAsync());
    }

    private sealed record AccountMenuPermissions(bool CanEdit, bool CanCreate);
    private sealed record JournalMenuPermissions(bool CanCreate, bool CanPost, bool CanReverse);
}

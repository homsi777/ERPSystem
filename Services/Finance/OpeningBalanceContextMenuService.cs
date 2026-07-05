using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Services.Documents;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Services.Finance;

/// <summary>قائمة مهام الأرصدة الافتتاحية — النقر بزر اليمين على صف القائمة.</summary>
public static class OpeningBalanceContextMenuService
{
    public static void Show(OpeningBalanceListDto row, UIElement placementTarget) =>
        _ = ShowAsync(row, placementTarget);

    private static async Task ShowAsync(OpeningBalanceListDto row, UIElement placementTarget)
    {
        var perms = await LoadPermissionsAsync();
        var menu = new ContextMenu
        {
            FlowDirection = FlowDirection.RightToLeft,
            MinWidth = 220,
            Padding = new Thickness(4),
            Background = (Brush)System.Windows.Application.Current.Resources["SurfaceBrush"]!,
            BorderBrush = (Brush)System.Windows.Application.Current.Resources["BorderBrush"]!,
            BorderThickness = new Thickness(1)
        };

        menu.Items.Add(BuildHeader(row));
        menu.Items.Add(new Separator());

        foreach (var action in BuildActions(row, perms))
        {
            var captured = action;
            var mi = new MenuItem
            {
                Header = BuildMenuHeader(captured.Label, captured.Icon, captured.Destructive),
                Tag = captured,
                IsEnabled = captured.Enabled,
                FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
                FontSize = 13,
                Padding = new Thickness(12, 8, 12, 8)
            };
            if (captured.Destructive)
                mi.Foreground = (Brush)System.Windows.Application.Current.Resources["DangerBrush"]!;

            mi.Click += (_, _) =>
            {
                if (!captured.Enabled) return;
                if (captured.RequiresConfirmation &&
                    !ConfirmationDialogService.ConfirmDangerous(captured.Label, row.Number))
                    return;
                OpeningBalanceQuickActionRouter.TryHandle(captured.ActionKey, row);
            };
            menu.Items.Add(mi);
        }

        menu.PlacementTarget = placementTarget;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    private static IReadOnlyList<MenuAction> BuildActions(OpeningBalanceListDto row, MenuPermissions perms)
    {
        var actions = new List<MenuAction>
        {
            new("فتح", "\uE8A7", "ob:open", true)
        };

        if (row.Status is OpeningBalanceStatus.Draft or OpeningBalanceStatus.Rejected)
            actions.Add(new("إرسال للاعتماد", "\uE7BA", "ob:submit", perms.CanEdit));

        if (row.Status is OpeningBalanceStatus.PendingApproval or OpeningBalanceStatus.Draft)
            actions.Add(new("اعتماد", "\uE73E", "ob:approve", perms.CanApprove));

        if (row.Status == OpeningBalanceStatus.Approved)
            actions.Add(new("ترحيل", "\uE8C1", "ob:post", perms.CanPost));

        if (row.Status != OpeningBalanceStatus.Archived)
            actions.Add(new("أرشفة", "\uE7B8", "ob:archive", perms.CanArchive, Destructive: true, RequiresConfirmation: true));

        actions.Add(new("تصدير Excel", "\uEDE1", "ob:export", perms.CanExport));
        return actions;
    }

    private static MenuItem BuildHeader(OpeningBalanceListDto row)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = row.Number,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Foreground = (Brush)System.Windows.Application.Current.Resources["TextPrimaryBrush"]!
        });
        sp.Children.Add(new TextBlock
        {
            Text = $"{row.TypeDisplay} • {row.StatusDisplay} • {row.TotalBaseAmount:N2}",
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0),
            Foreground = (Brush)System.Windows.Application.Current.Resources["TextMutedBrush"]!
        });
        return new MenuItem
        {
            Header = sp,
            IsEnabled = false,
            Padding = new Thickness(12, 10, 12, 10)
        };
    }

    private static object BuildMenuHeader(string label, string icon, bool destructive)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock
        {
            Text = icon,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = destructive
                ? (Brush)System.Windows.Application.Current.Resources["DangerBrush"]!
                : (Brush)System.Windows.Application.Current.Resources["TextSecondaryBrush"]!
        });
        sp.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        return sp;
    }

    private static async Task<MenuPermissions> LoadPermissionsAsync()
    {
        if (!AppServices.IsInitialized)
            return new MenuPermissions(false, false, false, false, false);

        var svc = OpeningBalanceUiService.Instance;
        return new MenuPermissions(
            await svc.CanAsync("openingbalances.edit"),
            await svc.CanAsync("openingbalances.approve"),
            await svc.CanAsync("openingbalances.post"),
            await svc.CanAsync("openingbalances.archive"),
            await svc.CanAsync("openingbalances.export"));
    }

    private sealed record MenuAction(
        string Label,
        string Icon,
        string ActionKey,
        bool Enabled,
        bool Destructive = false,
        bool RequiresConfirmation = false);

    private sealed record MenuPermissions(
        bool CanEdit,
        bool CanApprove,
        bool CanPost,
        bool CanArchive,
        bool CanExport);
}

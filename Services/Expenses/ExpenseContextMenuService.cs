using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Domain.Enums;
using ERPSystem.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Services.Expenses;

/// <summary>
/// قائمة مهام المصروف — تظهر عند النقر بزر اليمين على بطاقة التعريف.
/// </summary>
public static class ExpenseContextMenuService
{
    public static void Show(ExpenseListDto expense, UIElement placementTarget)
    {
        _ = ShowAsync(expense, placementTarget);
    }

    private static async Task ShowAsync(ExpenseListDto expense, UIElement placementTarget)
    {
        var perms = await LoadPermissionsAsync();
        var actions = EntityActionRegistry.GetActions(EntityType.Expense);
        if (actions.Count == 0) return;

        var menu = new ContextMenu
        {
            FlowDirection = FlowDirection.RightToLeft,
            MinWidth = 220,
            MaxHeight = 520,
            Padding = new Thickness(4),
            Background = (Brush)System.Windows.Application.Current.Resources["SurfaceBrush"]!,
            BorderBrush = (Brush)System.Windows.Application.Current.Resources["BorderBrush"]!,
            BorderThickness = new Thickness(1)
        };

        menu.Items.Add(BuildExpenseHeader(expense));
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
            var enabled = IsActionEnabled(captured.Id, expense, perms);
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

                var displayName = EntityDisplayNameResolver.Resolve(expense, EntityType.Expense);
                if (captured.RequiresConfirmation &&
                    !ConfirmationDialogService.ConfirmDangerous(captured.LabelAr, displayName))
                    return;

                ExpensePopupService.HandleAction(captured.Id, expense);
            };

            menu.Items.Add(mi);
        }

        menu.PlacementTarget = placementTarget;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    private static MenuItem BuildExpenseHeader(ExpenseListDto expense)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = expense.Name,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Foreground = (Brush)System.Windows.Application.Current.Resources["TextPrimaryBrush"]!
        });
        sp.Children.Add(new TextBlock
        {
            Text = $"{expense.Code} • {expense.StatusDisplay} • {expense.CategoryKindDisplay}",
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

    private static bool IsActionEnabled(EntityActionId id, ExpenseListDto expense, ExpenseMenuPermissions perms)
    {
        if (expense.IsArchived || expense.Status == ExpenseStatus.Archived)
        {
            return id is EntityActionId.ExpenseOperationsCenter
                or EntityActionId.ExpenseDetails
                or EntityActionId.ExpensePaymentHistory
                or EntityActionId.ExpenseAttachments
                or EntityActionId.ExpenseAuditHistory
                or EntityActionId.ExpenseTimeline
                or EntityActionId.ExpenseEntryLog
                or EntityActionId.ExpenseExportPdf
                or EntityActionId.ExpenseExportExcel
                or EntityActionId.ExpensePrint
                or EntityActionId.ExpenseShareReport;
        }

        if (expense.Status == ExpenseStatus.Cancelled)
        {
            return id is EntityActionId.ExpenseOperationsCenter
                or EntityActionId.ExpenseDetails
                or EntityActionId.ExpenseAuditHistory
                or EntityActionId.ExpenseTimeline
                or EntityActionId.ExpenseEntryLog
                or EntityActionId.ExpenseExportPdf
                or EntityActionId.ExpenseExportExcel
                or EntityActionId.ExpensePrint
                or EntityActionId.ExpenseArchive
                or EntityActionId.ExpenseDelete;
        }

        return id switch
        {
            EntityActionId.ExpenseEdit => perms.CanEdit,
            EntityActionId.ExpenseRecordPayment => perms.CanEdit,
            EntityActionId.ExpenseSchedulePayment => perms.CanEdit,
            EntityActionId.ExpenseApprove => perms.CanApprove,
            EntityActionId.ExpenseReject => perms.CanApprove,
            EntityActionId.ExpenseDuplicate => perms.CanCreate,
            EntityActionId.ExpenseCancel => perms.CanEdit,
            EntityActionId.ExpenseArchive => perms.CanArchive,
            EntityActionId.ExpenseDelete => perms.CanDelete,
            _ => true
        };
    }

    private static async Task<ExpenseMenuPermissions> LoadPermissionsAsync()
    {
        if (!AppServices.IsInitialized)
            return new ExpenseMenuPermissions(false, false, false, false, false);

        var svc = ExpenseUiService.Instance;
        return new ExpenseMenuPermissions(
            await svc.CanEditAsync(),
            await svc.CanCreateAsync(),
            await svc.CanApproveAsync(),
            await svc.CanArchiveAsync(),
            await svc.CanDeleteAsync());
    }

    private sealed record ExpenseMenuPermissions(
        bool CanEdit,
        bool CanCreate,
        bool CanApprove,
        bool CanArchive,
        bool CanDelete);
}

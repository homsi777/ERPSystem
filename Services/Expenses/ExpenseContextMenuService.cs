using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Domain.Enums;
using ERPSystem.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ERPSystem.Services.Expenses;

/// <summary>
/// قائمة مهام المصروف — تظهر عند النقر بزر اليمين على بطاقة التعريف أو القيد.
/// </summary>
public static class ExpenseContextMenuService
{
    private static readonly EntityActionId[] ExportActionOrder =
    [
        EntityActionId.ExpenseEntryLog,
        EntityActionId.ExpenseExportPdf,
        EntityActionId.ExpenseExportExcel,
        EntityActionId.ExpensePrint,
        EntityActionId.ExpenseShareReport
    ];

    public static void Show(ExpenseListDto expense, UIElement placementTarget)
    {
        _ = ShowAsync(expense, placementTarget);
    }

    private static async Task ShowAsync(ExpenseListDto expense, UIElement placementTarget)
    {
        var perms = await LoadPermissionsAsync();
        var actions = EntityActionRegistry.GetActions(EntityType.Expense);
        if (actions.Count == 0) return;

        var exportActions = actions
            .Where(a => ExportActionOrder.Contains(a.Id))
            .OrderBy(a => Array.IndexOf(ExportActionOrder, a.Id))
            .ToList();

        var mainActions = actions.Where(a => !ExportActionOrder.Contains(a.Id)).ToList();

        var menu = new ContextMenu
        {
            FlowDirection = FlowDirection.RightToLeft,
            MinWidth = 220,
            MaxHeight = 480,
            Padding = new Thickness(4),
            Background = Br("SurfaceBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(1),
            Placement = PlacementMode.Custom,
            CustomPopupPlacementCallback = (popupSize, _, _) =>
            {
                var mouse = Mouse.GetPosition(placementTarget);
                var screen = placementTarget.PointToScreen(mouse);
                var workArea = SystemParameters.WorkArea;
                var openUp = screen.Y + popupSize.Height > workArea.Bottom;
                var y = openUp ? Math.Max(0, mouse.Y - popupSize.Height) : mouse.Y;
                return [new CustomPopupPlacement(new Point(mouse.X, y), PopupPrimaryAxis.None)];
            }
        };

        menu.Items.Add(BuildExpenseHeader(expense));
        menu.Items.Add(new Separator());

        var exportSubmenu = BuildExportSubmenu(expense, perms, exportActions);
        var exportInserted = false;

        string? currentGroup = null;
        foreach (var action in mainActions)
        {
            var enabled = IsActionEnabled(action.Id, expense, perms);
            if (!enabled)
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

            menu.Items.Add(BuildActionItem(action, expense, enabled: true));

            if (action.Id == EntityActionId.ExpenseDetails && !exportInserted)
            {
                menu.Items.Add(new Separator());
                menu.Items.Add(exportSubmenu);
                exportInserted = true;
            }
        }

        if (!exportInserted)
        {
            menu.Items.Add(new Separator());
            menu.Items.Add(exportSubmenu);
        }

        menu.PlacementTarget = placementTarget;
        menu.IsOpen = true;
    }

    private static MenuItem BuildExportSubmenu(
        ExpenseListDto expense,
        ExpenseMenuPermissions perms,
        IReadOnlyList<EntityActionDefinition> exportActions)
    {
        var exportRoot = new MenuItem
        {
            Header = BuildMenuHeader("\uEDE1", "تصدير", destructive: false),
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 13,
            Padding = new Thickness(12, 8, 12, 8)
        };

        var anyEnabled = false;
        foreach (var action in exportActions)
        {
            var enabled = IsActionEnabled(action.Id, expense, perms);
            anyEnabled |= enabled;
            exportRoot.Items.Add(BuildActionItem(action, expense, enabled));
        }

        exportRoot.IsEnabled = anyEnabled || exportActions.Count == 0;
        return exportRoot;
    }

    private static MenuItem BuildActionItem(EntityActionDefinition action, ExpenseListDto expense, bool enabled)
    {
        var mi = new MenuItem
        {
            Header = BuildMenuHeader(action),
            Tag = action,
            IsEnabled = enabled,
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 13,
            Padding = new Thickness(12, 8, 12, 8)
        };

        if (action.IsDestructive)
            mi.Foreground = Br("DangerBrush");

        mi.Click += (_, _) =>
        {
            if (!enabled) return;

            var displayName = EntityDisplayNameResolver.Resolve(expense, EntityType.Expense);
            if (action.RequiresConfirmation &&
                !ConfirmationDialogService.ConfirmDangerous(action.LabelAr, displayName))
                return;

            ExpensePopupService.HandleAction(action.Id, expense);
        };

        return mi;
    }

    private static MenuItem BuildExpenseHeader(ExpenseListDto expense)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = expense.Name,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Foreground = Br("TextPrimaryBrush")
        });
        sp.Children.Add(new TextBlock
        {
            Text = $"{expense.Code} • {expense.StatusDisplay} • {expense.CategoryKindDisplay}",
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

    private static object BuildMenuHeader(EntityActionDefinition action) =>
        BuildMenuHeader(action.IconGlyph, action.LabelAr, action.IsDestructive);

    private static object BuildMenuHeader(string glyph, string label, bool destructive)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock
        {
            Text = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = destructive ? Br("DangerBrush") : Br("TextSecondaryBrush")
        });
        sp.Children.Add(new TextBlock
        {
            Text = label,
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

    private static Brush Br(string key) => (Brush)System.Windows.Application.Current.Resources[key]!;

    private sealed record ExpenseMenuPermissions(
        bool CanEdit,
        bool CanCreate,
        bool CanApprove,
        bool CanArchive,
        bool CanDelete);
}

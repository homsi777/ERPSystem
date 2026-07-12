using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Controls.Expenses;
using ERPSystem.Controls.Expenses.Popups;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Dialogs;
using ERPSystem.Domain.Enums;
using ERPSystem.Services;
using System.Windows;

namespace ERPSystem.Services.Expenses;

public static class ExpensePopupService
{
    private static ErpModalWindow? _active;

    public static bool HandleAction(EntityActionId actionId, ExpenseListDto expense)
    {
        switch (actionId)
        {
            case EntityActionId.ExpenseOperationsCenter:
                return ShowOperationsCenter(expense);

            case EntityActionId.ExpenseDetails:
                return ShowDetails(expense);

            case EntityActionId.ExpenseEdit:
                return ShowEdit(expense);

            case EntityActionId.ExpenseApprove:
                _ = ApproveAsync(expense);
                return true;

            case EntityActionId.ExpenseReject:
                _ = RejectAsync(expense);
                return true;

            case EntityActionId.ExpenseDuplicate:
                _ = DuplicateAsync(expense);
                return true;

            case EntityActionId.ExpenseArchive:
                _ = ArchiveAsync(expense);
                return true;

            case EntityActionId.ExpenseCancel:
                _ = CancelAsync(expense);
                return true;

            case EntityActionId.ExpenseDelete:
                _ = DeleteAsync(expense);
                return true;

            case EntityActionId.ExpensePaymentHistory:
                return ShowOperationsCenter(expense, "Payments");

            case EntityActionId.ExpenseRecordPayment:
                return ShowEntry(expense);

            case EntityActionId.ExpenseSchedulePayment:
                return ShowOperationsCenter(expense, "Installments");

            case EntityActionId.ExpenseAttachments:
                return ShowOperationsCenter(expense, "Attachments");

            case EntityActionId.ExpenseAuditHistory:
                return ShowOperationsCenter(expense, "Audit");

            case EntityActionId.ExpenseTimeline:
                return ShowOperationsCenter(expense, "Timeline");

            case EntityActionId.ExpenseEntryLog:
                ExpenseNavigationContext.BeginEntriesFor(expense.Id);
                MockInteractionService.Navigate(AppModule.Expenses, "Entries");
                return true;

            case EntityActionId.ExpenseExportPdf:
            case EntityActionId.ExpenseExportExcel:
            case EntityActionId.ExpensePrint:
            case EntityActionId.ExpenseShareReport:
                MockInteractionService.ShowDocumentPreview(
                    $"تقرير مصروف — {expense.Name}",
                    actionId == EntityActionId.ExpenseExportExcel ? "Excel" : "PDF");
                return true;

            default:
                return false;
        }
    }

    public static bool ShowCreate() =>
        ShowFormPopup("تعريف مصروف جديد", "عرّف مصروفاً بالاسم ثم سجّل قيوداً عليه", "\uE9D9", 480, () =>
        {
            ExpenseNavigationContext.BeginCreate();
            return new ExpenseFormControl();
        });

    public static bool ShowEdit(ExpenseListDto expense) =>
        ShowFormPopup("تعديل تعريف", expense.Name, "\uE70F", 480, () =>
        {
            ExpenseNavigationContext.BeginEdit(expense.Id);
            return new ExpenseFormControl();
        });

    public static bool ShowEntry(ExpenseListDto? preselected = null)
    {
        if (preselected is not null)
            ExpenseNavigationContext.BeginEntryFor(preselected.Id);

        var form = new ExpenseEntryFormControl();
        form.BindPopupHost(hideExpensePicker: preselected is not null);
        var subtitle = preselected?.Name ?? "تسجيل حركة صرف يومية";
        return ShowDialog(form, "قيد مصروف جديد", subtitle, "\uE719", 580, 760) == true;
    }

    public static bool ShowDetails(ExpenseListDto expense) =>
        ShowDetailsById(expense.Id, expense.Name, expense.Code);

    public static bool ShowDetailsById(Guid expenseId, string? name = null, string? code = null) =>
        ShowDialog(new ExpenseDetailsPopupControl(expenseId),
            "تفاصيل المصروف", $"{name ?? "—"} — {code ?? expenseId.ToString()[..8]}", "\uE7B3", 560) == true;

    public static bool ShowOperationsCenter(ExpenseListDto expense, string? initialTab = null)
    {
        var oc = new ExpenseOperationsCenterControl();
        oc.Initialize(expense.Id, initialTab);
        return ShowDialog(oc, "مركز العمليات", $"{expense.Name} — {expense.Code}", "\uE8A7", 920, 840) == true;
    }

    public static void CompleteSuccess()
    {
        ExpenseListRefreshHub.RequestRefresh();
        if (_active is null) return;
        _active.DialogResult = true;
        _active.Close();
    }

    public static void CancelActive()
    {
        if (_active is null) return;
        _active.DialogResult = false;
        _active.Close();
    }

    public static ExpenseListDto FromEntry(ExpenseEntryListDto entry) => new()
    {
        Id = entry.ExpenseId,
        Code = entry.ExpenseCode,
        Name = entry.ExpenseName
    };

    private static bool ShowFormPopup(
        string title,
        string subtitle,
        string icon,
        double width,
        Func<ExpenseFormControl> factory)
    {
        var form = factory();
        form.BindPopupHost();
        return ShowDialog(form, title, subtitle, icon, width) == true;
    }

    private static bool? ShowDialog(
        UIElement content,
        string title,
        string subtitle,
        string icon,
        double width,
        double maxHeight = 680)
    {
        _active = new ErpModalWindow();
        _active.Configure(title, subtitle, icon, width, maxHeight);
        _active.SetBody(content);
        var result = _active.ShowDialog();
        _active = null;
        ExpenseNavigationContext.ClearPreselection();
        return result;
    }

    private static async Task ApproveAsync(ExpenseListDto row)
    {
        if (!await ExpenseUiService.Instance.CanApproveAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية الاعتماد.", "صلاحية");
            return;
        }
        var result = await ExpenseUiService.Instance.ApproveAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
            ExpenseListRefreshHub.RequestRefresh();
    }

    private static async Task RejectAsync(ExpenseListDto row)
    {
        if (!await ExpenseUiService.Instance.CanApproveAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية الرفض.", "صلاحية");
            return;
        }
        var result = await ExpenseUiService.Instance.RejectAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
            ExpenseListRefreshHub.RequestRefresh();
    }

    private static async Task DuplicateAsync(ExpenseListDto row)
    {
        if (!await ExpenseUiService.Instance.CanCreateAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية إنشاء مصاريف.", "صلاحية");
            return;
        }
        var result = await ExpenseUiService.Instance.DuplicateAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
        {
            ExpenseListRefreshHub.RequestRefresh();
            MockInteractionService.ShowSuccess("تم نسخ المصروف بنجاح.");
        }
    }

    private static async Task ArchiveAsync(ExpenseListDto row)
    {
        if (!await ExpenseUiService.Instance.CanArchiveAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية الأرشفة.", "صلاحية");
            return;
        }
        var result = await ExpenseUiService.Instance.ArchiveAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
            ExpenseListRefreshHub.RequestRefresh();
    }

    private static async Task CancelAsync(ExpenseListDto row)
    {
        if (!await ExpenseUiService.Instance.CanEditAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية الإلغاء.", "صلاحية");
            return;
        }
        var result = await ExpenseUiService.Instance.CancelAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
            ExpenseListRefreshHub.RequestRefresh();
    }

    private static async Task DeleteAsync(ExpenseListDto row)
    {
        if (!await ExpenseUiService.Instance.CanDeleteAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية الحذف.", "صلاحية");
            return;
        }
        if (MessageBox.Show($"حذف المصروف «{row.Name}» نهائياً؟", "تأكيد الحذف",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var result = await ExpenseUiService.Instance.DeleteAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
            ExpenseListRefreshHub.RequestRefresh();
    }
}

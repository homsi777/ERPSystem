using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Core;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Services.Documents;

namespace ERPSystem.Services.Finance;

public static class OpeningBalanceQuickActionRouter
{
    public static bool TryHandleQuickAction(string? actionKey, OperationsCenterContext ctx)
    {
        if (string.IsNullOrEmpty(actionKey) || ctx.EntityRow is not OpeningBalanceListDto row)
            return false;

        return TryHandle(actionKey, row);
    }

    public static bool TryHandle(string actionKey, OpeningBalanceListDto row) => actionKey switch
    {
        "ob:submit" => Run(() => SubmitAsync(row)),
        "ob:approve" => Run(() => ApproveAsync(row)),
        "ob:post" => Run(() => PostAsync(row)),
        "ob:archive" => Run(() => ArchiveAsync(row)),
        "ob:export" => Run(() => ExportAsync(row)),
        "ob:open" => Run(() => OpenAsync(row)),
        "nav:Accounting:OpeningBalances" => NavigateList(),
        "nav:Accounting:OpeningBalanceWorkspace" => NavigateWorkspace(row),
        _ => false
    };

    private static bool Run(Func<Task> action)
    {
        _ = action();
        return true;
    }

    private static bool NavigateList()
    {
        MockInteractionService.Navigate(AppModule.Accounting, "OpeningBalances");
        return true;
    }

    private static bool NavigateWorkspace(OpeningBalanceListDto row)
    {
        OpeningBalanceNavigationContext.BeginWorkspace(row.Id);
        MockInteractionService.Navigate(AppModule.Accounting, "OpeningBalanceWorkspace");
        return true;
    }

    private static Task OpenAsync(OpeningBalanceListDto row)
    {
        OpeningBalancePopupService.ShowOperationsCenter(row);
        return Task.CompletedTask;
    }

    private static async Task SubmitAsync(OpeningBalanceListDto row)
    {
        if (row.Status is not (OpeningBalanceStatus.Draft or OpeningBalanceStatus.Rejected))
        {
            MockInteractionService.ShowWarning("لا يمكن إرسال هذا المستند للاعتماد في حالته الحالية.");
            return;
        }

        var result = await OpeningBalanceUiService.Instance.SubmitAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
        {
            OpeningBalanceListRefreshHub.RequestRefresh();
            MockInteractionService.ShowSuccess("تم إرسال المستند للاعتماد.");
        }
    }

    private static async Task ApproveAsync(OpeningBalanceListDto row)
    {
        if (row.Status is not (OpeningBalanceStatus.PendingApproval or OpeningBalanceStatus.Draft))
        {
            MockInteractionService.ShowWarning("لا يمكن اعتماد هذا المستند في حالته الحالية.");
            return;
        }

        var result = await OpeningBalanceUiService.Instance.ApproveAsync(row.Id, null);
        if (ApplicationResultPresenter.Present(result))
        {
            OpeningBalanceListRefreshHub.RequestRefresh();
            MockInteractionService.ShowSuccess("تم اعتماد المستند.");
        }
    }

    private static async Task PostAsync(OpeningBalanceListDto row)
    {
        if (row.Status != OpeningBalanceStatus.Approved)
        {
            MockInteractionService.ShowWarning("يجب اعتماد المستند قبل الترحيل.");
            return;
        }

        var result = await OpeningBalanceUiService.Instance.PostAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
        {
            OpeningBalanceListRefreshHub.RequestRefresh();
            MockInteractionService.ShowSuccess(
                result.Value?.JournalEntryNumber is { Length: > 0 } num
                    ? $"تم الترحيل — القيد {num}"
                    : "تم ترحيل المستند.");
        }
    }

    private static async Task ArchiveAsync(OpeningBalanceListDto row)
    {
        if (row.Status == OpeningBalanceStatus.Archived)
        {
            MockInteractionService.ShowWarning("المستند مؤرشف مسبقاً.");
            return;
        }

        var result = await OpeningBalanceUiService.Instance.ArchiveAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
        {
            OpeningBalanceListRefreshHub.RequestRefresh();
            MockInteractionService.ShowSuccess("تم أرشفة المستند.");
        }
    }

    private static async Task ExportAsync(OpeningBalanceListDto row)
    {
        var result = await OpeningBalanceUiService.Instance.GetDetailsAsync(row.Id);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        var oc = result.Value;
        ListExportService.ExportRecords(
            oc.Lines,
            $"OpeningBalance-{oc.Header.Number}",
            ("السطر", l => l.LineNumber),
            ("الطرف", l => l.PartyName),
            ("الحساب", l => l.AccountName),
            ("مدين", l => l.Debit),
            ("دائن", l => l.Credit),
            ("الوصف", l => l.Description));
    }
}

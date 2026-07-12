using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Controls.Accounting;
using ERPSystem.Controls.Accounting.Popups;
using ERPSystem.Core.Actions;
using ERPSystem.Dialogs;
using ERPSystem.Services;
using System.Windows;

namespace ERPSystem.Services.Accounting;

public static class AccountingPopupService
{
    private static ErpModalWindow? _active;

    public static bool HandleAccountAction(EntityActionId actionId, AccountListDto account)
    {
        switch (actionId)
        {
            case EntityActionId.AccountDetails:
                return ShowAccountDetails(account);

            case EntityActionId.AccountEdit:
                return ShowEditAccount(account);

            case EntityActionId.AccountAddChild:
                return ShowCreateAccount(account.Id);

            case EntityActionId.AccountCreate:
                return ShowCreateAccount();

            case EntityActionId.AccountDeactivate:
                _ = DeactivateAccountAsync(account);
                return true;

            case EntityActionId.AccountLedger:
                return ShowAccountLedger(account);

            default:
                return false;
        }
    }

    public static bool HandleJournalAction(EntityActionId actionId, JournalEntryListDto entry)
    {
        switch (actionId)
        {
            case EntityActionId.JournalView:
            case EntityActionId.JournalDetails:
                return ShowJournalDetails(entry);

            case EntityActionId.JournalCreate:
                return ShowCreateJournal();

            case EntityActionId.JournalApprove:
                _ = ApproveJournalAsync(entry);
                return true;

            case EntityActionId.JournalPost:
                _ = PostJournalAsync(entry);
                return true;

            case EntityActionId.JournalReverse:
                _ = ReverseJournalAsync(entry);
                return true;

            case EntityActionId.JournalCancel:
                _ = CancelJournalAsync(entry);
                return true;

            case EntityActionId.VoucherPrint:
            case EntityActionId.VoucherExportPdf:
                _ = ExportJournalAsync(entry, actionId == EntityActionId.VoucherExportPdf);
                return true;

            default:
                return false;
        }
    }

    public static bool ShowCreateAccount(Guid? parentId = null) =>
        ShowAccountFormPopup("حساب جديد", "إضافة حساب إلى دليل الحسابات", "\uE710", 520, () =>
        {
            AccountingNavigationContext.BeginCreate(parentId);
            return new AccountFormControl();
        });

    public static bool ShowEditAccount(AccountListDto account) =>
        ShowAccountFormPopup("تعديل حساب", $"{account.Code} — {account.NameAr}", "\uE70F", 520, () =>
        {
            AccountingNavigationContext.BeginEdit(account.Id);
            return new AccountFormControl();
        });

    public static bool ShowAccountDetails(AccountListDto account) =>
        ShowDialog(new AccountDetailsPopupControl(account.Id),
            "تفاصيل الحساب", $"{account.Code} — {account.NameAr}", "\uE8C3", 560) == true;

    public static bool ShowAccountLedger(AccountListDto account)
    {
        var control = new AccountLedgerReportControl(account.Id);
        return ShowDialog(control, "كشف حساب", $"{account.Code} — {account.NameAr}", "\uE8A1", 900, 720) == true;
    }

    public static bool ShowCreateJournal()
    {
        AccountingNavigationContext.BeginJournalCreate();
        var form = new JournalEntryFormControl();
        form.BindPopupHost();
        return ShowDialog(form, "قيد يومية جديد", "إنشاء قيد يدوي متوازن", "\uE8C1", 840, 780) == true;
    }

    public static bool ShowJournalDetails(JournalEntryListDto entry) =>
        ShowDialog(new JournalEntryDetailsPopupControl(entry.Id),
            "تفاصيل القيد", $"{entry.EntryNumber} — {entry.EntryDate:yyyy/MM/dd}", "\uE7B3", 680, 760) == true;

    public static void CompleteSuccess()
    {
        AccountingListRefreshHub.RequestRefresh();
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

    private static bool ShowAccountFormPopup(
        string title,
        string subtitle,
        string icon,
        double width,
        Func<AccountFormControl> factory)
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
        AccountingNavigationContext.Clear();
        return result;
    }

    private static async Task ExportJournalAsync(JournalEntryListDto entry, bool exportPdf)
    {
        var result = await AccountingUiService.Instance.GetJournalDetailsAsync(entry.Id);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        AccountingJournalDocumentService.ShowPreview(result.Value, exportPdf);
    }

    private static async Task DeactivateAccountAsync(AccountListDto account)
    {
        if (!await AccountingUiService.Instance.CanEditAccountAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية تعطيل الحسابات.", "صلاحية");
            return;
        }

        if (MessageBox.Show($"تعطيل الحساب «{account.NameAr}»؟", "تأكيد",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var result = await AccountingUiService.Instance.DeactivateAccountAsync(account.Id);
        if (ApplicationResultPresenter.Present(result))
            AccountingListRefreshHub.RequestRefresh();
    }

    private static async Task ApproveJournalAsync(JournalEntryListDto entry)
    {
        if (!await AccountingUiService.Instance.CanPostJournalAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية الاعتماد.", "صلاحية");
            return;
        }

        var result = await AccountingUiService.Instance.ApproveJournalAsync(entry.Id);
        if (ApplicationResultPresenter.Present(result))
            AccountingListRefreshHub.RequestRefresh();
    }

    private static async Task PostJournalAsync(JournalEntryListDto entry)
    {
        if (!await AccountingUiService.Instance.CanPostJournalAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية الترحيل.", "صلاحية");
            return;
        }

        var result = await AccountingUiService.Instance.PostJournalAsync(entry.Id);
        if (ApplicationResultPresenter.Present(result))
            AccountingListRefreshHub.RequestRefresh();
    }

    private static async Task ReverseJournalAsync(JournalEntryListDto entry)
    {
        if (!await AccountingUiService.Instance.CanReverseJournalAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية عكس القيود.", "صلاحية");
            return;
        }

        if (MessageBox.Show($"إنشاء قيد عكسي لـ «{entry.EntryNumber}»؟", "تأكيد",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var result = await AccountingUiService.Instance.ReverseJournalAsync(entry.Id);
        if (ApplicationResultPresenter.Present(result))
        {
            AccountingListRefreshHub.RequestRefresh();
            MockInteractionService.ShowSuccess("تم إنشاء القيد العكسي.");
        }
    }

    private static async Task CancelJournalAsync(JournalEntryListDto entry)
    {
        if (!await AccountingUiService.Instance.CanCreateJournalAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية الإلغاء.", "صلاحية");
            return;
        }

        if (MessageBox.Show($"إلغاء القيد «{entry.EntryNumber}»؟", "تأكيد",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var result = await AccountingUiService.Instance.CancelJournalAsync(entry.Id);
        if (ApplicationResultPresenter.Present(result))
            AccountingListRefreshHub.RequestRefresh();
    }
}

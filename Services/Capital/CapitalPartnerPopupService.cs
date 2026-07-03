using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Controls.Capital;
using ERPSystem.Controls.Capital.Popups;
using ERPSystem.Core.Actions;
using ERPSystem.Dialogs;
using ERPSystem.Services;
using System.Windows;

namespace ERPSystem.Services.Capital;

public static class CapitalPartnerPopupService
{
    private static ErpModalWindow? _active;

    public static bool HandleAction(EntityActionId actionId, CapitalPartnerListDto partner)
    {
        switch (actionId)
        {
            case EntityActionId.CapitalPartnerOperationsCenter:
                return ShowOperationsCenter(partner);

            case EntityActionId.CapitalPartnerDetails:
                return ShowDetails(partner);

            case EntityActionId.CapitalPartnerLedger:
                return ShowOperationsCenter(partner, "Ledger");

            case EntityActionId.CapitalPartnerEdit:
                return ShowEdit(partner);

            case EntityActionId.CapitalPartnerNewInvestment:
                return ShowInvestment(partner, "Investment");

            case EntityActionId.CapitalPartnerWithdrawal:
                return ShowInvestment(partner, "Withdrawal");

            case EntityActionId.CapitalPartnerProfitDistribution:
                return ShowDistributions();

            case EntityActionId.CapitalPartnerAuditHistory:
                return ShowOperationsCenter(partner, "Audit");

            case EntityActionId.CapitalPartnerTimeline:
                return ShowOperationsCenter(partner, "Timeline");

            case EntityActionId.CapitalPartnerExportPdf:
            case EntityActionId.CapitalPartnerExportExcel:
            case EntityActionId.CapitalPartnerPrint:
                MockInteractionService.ShowDocumentPreview(
                    $"تقرير شريك — {partner.FullName}",
                    actionId == EntityActionId.CapitalPartnerExportExcel ? "Excel" : "PDF");
                return true;

            case EntityActionId.CapitalPartnerArchive:
                _ = ArchiveAsync(partner);
                return true;

            default:
                return false;
        }
    }

    public static bool ShowCreate() =>
        ShowFormPopup("شريك جديد", "تسجيل شريك مع نسبة الملكية ومبلغ الدخول", "\uE716", 500, () =>
        {
            CapitalNavigationContext.BeginCreate();
            return new CapitalPartnerFormControl();
        });

    public static bool ShowEdit(CapitalPartnerListDto partner) =>
        ShowFormPopup("تعديل شريك", partner.FullName, "\uE70F", 500, () =>
        {
            CapitalNavigationContext.BeginEdit(partner.Id);
            return new CapitalPartnerFormControl();
        });

    public static bool ShowInvestment(CapitalPartnerListDto partner, string mode)
    {
        CapitalNavigationContext.BeginTransaction(partner.Id, mode);
        var form = new CapitalInvestmentFormControl();
        form.BindPopupHost(hidePartnerPicker: true);
        var title = mode == "Withdrawal" ? "سحب رأس مال" : "استثمار رأس مال";
        var icon = mode == "Withdrawal" ? "\uE719" : "\uE710";
        return ShowDialog(form, title, partner.FullName, icon, 480) == true;
    }

    public static bool ShowDetails(CapitalPartnerListDto partner) =>
        ShowDialog(new CapitalPartnerDetailsPopupControl(partner.Id),
            "تفاصيل الشريك", $"{partner.FullName} — {partner.Code}", "\uE7B3", 560) == true;

    public static bool ShowOperationsCenter(CapitalPartnerListDto partner, string? initialTab = null)
    {
        var oc = new CapitalOperationsCenterControl();
        oc.Initialize(partner.Id, initialTab);
        return ShowDialog(oc, "مركز العمليات", $"{partner.FullName} — {partner.Code}", "\uE8A7", 920, 840) == true;
    }

    public static bool ShowLedger(CapitalPartnerListDto partner) =>
        ShowDialog(new CapitalPartnerLedgerPopupControl(partner.Id),
            "دفتر الاستثمار", partner.FullName, "\uE8A1", 620, 720) == true;

    public static bool ShowAudit(CapitalPartnerListDto partner) =>
        ShowDialog(new CapitalPartnerAuditPopupControl(partner.Id),
            "سجل التدقيق", partner.FullName, "\uE7C3", 600, 720) == true;

    public static bool ShowTimeline(CapitalPartnerListDto partner) =>
        ShowDialog(new CapitalPartnerTimelinePopupControl(partner.Id),
            "الخط الزمني", partner.FullName, "\uE823", 600, 720) == true;

    public static bool ShowDistributions()
    {
        var content = new CapitalDistributionsControl();
        content.BindPopupHost();
        return ShowDialog(content, "توزيع الأرباح",
            "إدارة توزيعات الأرباح على الشركاء", "\uE8C1", 640, 720) == true;
    }

    public static void CompleteSuccess()
    {
        CapitalListRefreshHub.RequestRefresh();
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

    private static bool ShowFormPopup(
        string title,
        string subtitle,
        string icon,
        double width,
        Func<CapitalPartnerFormControl> factory)
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
        CapitalNavigationContext.ClearTransactionContext();
        return result;
    }

    private static async Task ArchiveAsync(CapitalPartnerListDto partner)
    {
        if (!await CapitalPartnerUiService.Instance.CanArchiveAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية الأرشفة.", "صلاحية");
            return;
        }

        var result = await CapitalPartnerUiService.Instance.ArchivePartnerAsync(partner.Id);
        if (ApplicationResultPresenter.Present(result))
            CapitalListRefreshHub.RequestRefresh();
    }
}

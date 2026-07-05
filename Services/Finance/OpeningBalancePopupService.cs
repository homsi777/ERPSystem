using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Controls.Finance;
using ERPSystem.Dialogs;
using ERPSystem.Services.Finance;

namespace ERPSystem.Services.Finance;

public static class OpeningBalancePopupService
{
    public static bool ShowOperationsCenter(OpeningBalanceListDto row, string? initialTab = null)
    {
        var oc = new OpeningBalanceOperationsCenterControl();
        oc.Initialize(row.Id, initialTab);
        return ErpModalWindow.Show(
            "مركز عمليات الرصيد الافتتاحي",
            $"{row.Number} — {row.TypeDisplay}",
            oc,
            "\uE8F1",
            920,
            840) == true;
    }
}

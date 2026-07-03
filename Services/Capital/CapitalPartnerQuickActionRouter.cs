using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Core.Actions;

namespace ERPSystem.Services.Capital;

public static class CapitalPartnerQuickActionRouter
{
    public static bool TryHandle(string actionKey, CapitalPartnerDetailsDto partner)
    {
        var row = ToListDto(partner);
        return actionKey switch
        {
            "nav:CapitalPartners:Form" => CapitalPartnerPopupService.ShowEdit(row),
            "capital:investment" => CapitalPartnerPopupService.ShowInvestment(row, "Investment"),
            "capital:withdrawal" => CapitalPartnerPopupService.ShowInvestment(row, "Withdrawal"),
            "capital:archive" => CapitalPartnerPopupService.HandleAction(EntityActionId.CapitalPartnerArchive, row),
            _ => false
        };
    }

    private static CapitalPartnerListDto ToListDto(CapitalPartnerDetailsDto d) => new()
    {
        Id = d.Id,
        Code = d.Code,
        FullName = d.FullName,
        Status = d.Status,
        StatusDisplay = d.StatusDisplay,
        CurrentCapitalBase = d.CurrentCapitalBase,
        TotalInvestmentsBase = d.TotalInvestmentsBase,
        TotalWithdrawalsBase = d.TotalWithdrawalsBase
    };
}

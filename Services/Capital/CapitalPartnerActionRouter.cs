using ERPSystem.Application.DTOs.Capital;

using ERPSystem.Core;

using ERPSystem.Core.Actions;

using ERPSystem.Services;



namespace ERPSystem.Services.Capital;



public static class CapitalPartnerActionRouter

{

    public static bool TryHandle(EntityActionId actionId, EntityType entityType, object entityRow, AppModule sourceModule)

    {

        if (entityType != EntityType.CapitalPartner || entityRow is not CapitalPartnerListDto row)

            return false;



        return CapitalPartnerPopupService.HandleAction(actionId, row);

    }

}


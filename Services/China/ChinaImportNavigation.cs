using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Controls.China;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.ChinaImport;
using ERPSystem.Core.Workspace;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services.China;

namespace ERPSystem.Services;

public static class ChinaImportNavigation
{
    public static void Navigate(string subPage, ChinaContainerStatus? containerStatus = null)
    {
        var containerId = ChinaImportNavigationContext.ResolveContainerId();
        var hasParse = ChinaImportNavigationContext.GetParseResult() is not null;
        var unitConfirmed = ChinaImportNavigationContext.IsDplQuantityUnitConfirmed;

        if (!ChinaImportWorkflow.CanAccessRoute(subPage, containerStatus, hasParse, containerId, unitConfirmed))
        {
            MockInteractionService.ShowWarning(
                "لا يمكن الانتقال إلى هذه الخطوة قبل إكمال المراحل السابقة.",
                "سير عمل الاستيراد");
            return;
        }

        NavigationStateManager.Instance.NavigateTo(AppModule.ChinaImport, subPage);
    }

    public static void OpenOperationsCenter(ContainerListRow row, string initialTab = "Overview")
    {
        ChinaImportNavigationContext.SetActiveContainer(row.Id);
        WorkspaceWindowManager.Instance.OpenAction(
            EntityActionId.OpenOperationsCenter,
            EntityType.ImportContainer,
            row,
            AppModule.ChinaImport);
    }

    public static void OpenLandingCostWorkspace(ContainerListRow row) =>
        OpenOperationsCenter(row, "LandingCost");

    public static void OpenWorkflowForContainer(ContainerListRow row)
    {
        ChinaImportNavigationContext.SetActiveContainer(row.Id);
        Navigate(ChinaImportWorkflow.ResolveRouteForStatus(row.Status), row.Status);
    }

    public static async Task OpenOperationsCenterByIdAsync(Guid containerId)
    {
        if (!AppServices.IsInitialized)
            return;

        var result = await ContainerUiService.Instance.GetOperationsCenterAsync(containerId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
        {
            MockInteractionService.ShowWarning("تعذّر فتح الحاوية المرتبطة.", "استيراد الصين");
            return;
        }

        var c = result.Value.Container;
        var row = ContainerListRow.FromDto(new ContainerListDto
        {
            Id = c.Id,
            ContainerNumber = c.ContainerNumber,
            Status = c.Status,
            ShipmentDate = c.ShipmentDate,
            ExpectedArrival = c.ArrivalDate,
            TotalRolls = c.TotalRolls,
            TotalMeters = c.TotalMeters,
            TotalWeightKg = c.TotalWeightKg,
            SupplierName = c.SupplierName
        });
        OpenOperationsCenter(row);
    }
}

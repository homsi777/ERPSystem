using ERPSystem.Controls.China;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.ChinaImport;
using ERPSystem.Core.Workspace;
using ERPSystem.Domain.Enums;
using ERPSystem.Services.China;

namespace ERPSystem.Services;

public static class ChinaImportNavigation
{
    public static void Navigate(string subPage, ChinaContainerStatus? containerStatus = null)
    {
        var containerId = ChinaImportNavigationContext.ResolveContainerId();
        var hasParse = ChinaImportNavigationContext.GetParseResult() is not null;

        if (!ChinaImportWorkflow.CanAccessRoute(subPage, containerStatus, hasParse, containerId))
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
}

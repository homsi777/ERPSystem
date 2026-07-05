using ERPSystem.Controls.China;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Workspace;
using ERPSystem.Helpers;
using ERPSystem.Services.China;

namespace ERPSystem.Services;

public static class ChinaContainerActionRouter
{
    public static bool TryHandle(EntityActionId actionId, EntityType entityType, object entityRow, AppModule sourceModule)
    {
        if (entityType != EntityType.ImportContainer || entityRow is not ContainerListRow row)
            return false;

        switch (actionId)
        {
            case EntityActionId.ContainerArchive:
                _ = ArchiveAsync(row.Id);
                return true;

            case EntityActionId.ContainerDelete:
                MockInteractionService.ShowWarning(
                    "حذف الحاوية غير متاح. استخدم «أرشفة الحاوية» لإخفائها من القائمة.",
                    "حذف الحاوية");
                return true;

            case EntityActionId.ContainerApprove:
            case EntityActionId.ContainerCosts:
            case EntityActionId.ContainerDistribution:
            case EntityActionId.ContainerStocktake:
                ChinaImportNavigationContext.SetActiveContainer(row.Id);
                WorkspaceWindowManager.Instance.OpenAction(actionId, entityType, row, sourceModule);
                return true;

            case EntityActionId.ContainerDocumentation:
                ContainerDocumentationPopupService.Show(row);
                return true;

            default:
                return false;
        }
    }

    private static async Task ArchiveAsync(Guid containerId)
    {
        if (!AppServices.IsInitialized)
            return;

        var result = await ContainerUiService.Instance.ArchiveContainerAsync(containerId);
        if (ApplicationResultPresenter.Present(result))
        {
            ContainerListRefreshHub.RequestRefresh();
            ErpDataRefreshHub.RequestRefresh(ErpDataRefreshScope.OperationsCenter);
            MockInteractionService.ShowSuccess("تم أرشفة الحاوية.", "أرشفة الحاوية");
        }
    }
}

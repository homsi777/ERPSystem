using ERPSystem.Controls.China;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Workspace;
using ERPSystem.Helpers;
using ERPSystem.Services.China;
using System.Windows.Controls;

namespace ERPSystem.Services;

public static class ChinaContainerQuickActionRouter
{
    public static bool TryHandle(string? actionKey, OperationsCenterContext ctx, TabControl? tabs)
    {
        if (ctx.EntityType != EntityType.ImportContainer || ctx.EntityRow is not ContainerListRow row)
            return false;

        if (string.IsNullOrEmpty(actionKey))
            return false;

        if (actionKey.StartsWith("tab:", StringComparison.OrdinalIgnoreCase))
        {
            SelectTab(tabs, actionKey[4..]);
            return true;
        }

        switch (actionKey)
        {
            case "china:Approve":
                _ = ApproveAsync(row.Id);
                return true;
            case "china:Archive":
                _ = ArchiveAsync(row.Id);
                return true;
            case "china:MoveToWarehouse":
                ChinaImportNavigationContext.SetActiveContainer(row.Id);
                ChinaImportNavigation.Navigate("MoveToWarehouse", row.Status);
                return true;
            case "china:SalePrice":
                ChinaImportNavigationContext.SetActiveContainer(row.Id);
                ChinaImportNavigation.Navigate("SalePrice", row.Status);
                return true;
            case "china:Documentation":
                ContainerDocumentationPopupService.Show(row);
                return true;
            case "nav:ChinaImport:NewImport":
                ChinaImportNavigation.Navigate("NewImport");
                return true;
            case "ws:LandingCost":
                SelectTab(tabs, "LandingCost");
                return true;
            case "preview:ملخص الحاوية":
                MockInteractionService.ShowDocumentPreview($"ملخص الحاوية {row.ContainerNumber}", "PDF");
                return true;
            default:
                return false;
        }
    }

    private static async Task ApproveAsync(Guid containerId)
    {
        if (!AppServices.IsInitialized)
            return;

        try
        {
            var result = await ContainerUiService.Instance.ApproveContainerAsync(containerId);
            if (ApplicationResultPresenter.Present(result))
            {
                ContainerListRefreshHub.RequestRefresh();
                ErpDataRefreshHub.RequestRefresh(ErpDataRefreshScope.OperationsCenter);
                MockInteractionService.ShowSuccess("تم اعتماد الحاوية بنجاح.", "اعتماد الحاوية");
            }
        }
        catch (Exception ex)
        {
            MockInteractionService.ShowWarning($"تعذّر اعتماد الحاوية.\n\n{ex.Message}", "اعتماد الحاوية");
        }
    }

    private static async Task ArchiveAsync(Guid containerId)
    {
        if (!AppServices.IsInitialized)
            return;

        try
        {
            var result = await ContainerUiService.Instance.ArchiveContainerAsync(containerId);
            if (ApplicationResultPresenter.Present(result))
            {
                ContainerListRefreshHub.RequestRefresh();
                ErpDataRefreshHub.RequestRefresh(ErpDataRefreshScope.OperationsCenter);
                MockInteractionService.ShowSuccess("تم أرشفة الحاوية.", "أرشفة");
            }
        }
        catch (Exception ex)
        {
            MockInteractionService.ShowWarning($"تعذّر أرشفة الحاوية.\n\n{ex.Message}", "أرشفة");
        }
    }

    private static void SelectTab(TabControl? tabs, string tabKey)
    {
        if (tabs is null)
            return;

        for (var i = 0; i < tabs.Items.Count; i++)
        {
            if (tabs.Items[i] is TabItem ti &&
                ti.Tag is string key &&
                key.Equals(tabKey, StringComparison.OrdinalIgnoreCase))
            {
                tabs.SelectedIndex = i;
                return;
            }
        }
    }
}

using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Services;
using System.Windows;

namespace ERPSystem.Services.Inventory;

public static class InventoryActionRouter
{
    public static bool TryHandle(EntityActionId actionId, EntityType entityType, object entityRow, AppModule sourceModule)
    {
        if (entityType != EntityType.Warehouse || entityRow is not WarehouseListExtendedDto row)
            return false;

        Handle(actionId, row, sourceModule);
        return true;
    }

    public static void Handle(EntityActionId actionId, WarehouseListExtendedDto row, AppModule sourceModule)
    {
        switch (actionId)
        {
            case EntityActionId.OpenOperationsCenter:
                OpenOperationsCenter(row);
                break;
            case EntityActionId.WarehouseEdit:
            case EntityActionId.WarehouseProperties:
                InventoryPopupService.ShowEditWarehouse(row.Id);
                break;
            case EntityActionId.FabricTransfer:
                InventoryPopupService.ShowTransferWizard(row.Id);
                break;
            case EntityActionId.WarehouseStockReport:
                OpenOperationsCenter(row, "Stock");
                break;
            case EntityActionId.FabricMovement:
            case EntityActionId.WarehouseMovementHistory:
                OpenOperationsCenter(row, "Movements");
                break;
            case EntityActionId.WarehouseStocktake:
                InventoryPopupService.ShowStocktakeWizard(row.Id);
                break;
            case EntityActionId.WarehouseTimeline:
                OpenOperationsCenter(row, "Timeline");
                break;
            case EntityActionId.WarehouseAudit:
                OpenOperationsCenter(row, "Audit");
                break;
            case EntityActionId.WarehouseExportExcel:
            case EntityActionId.WarehouseExportPdf:
            case EntityActionId.WarehousePrint:
                InventoryExportService.ExportWarehouseStock(row);
                break;
            case EntityActionId.WarehouseDuplicate:
                _ = DuplicateAsync(row);
                break;
            case EntityActionId.WarehouseArchive:
                _ = ArchiveAsync(row);
                break;
            case EntityActionId.WarehouseActivate:
                _ = ActivateAsync(row);
                break;
        }
    }

    public static bool TryHandleQuickAction(string? actionKey, OperationsCenterContext ctx)
    {
        if (ctx.EntityRow is not WarehouseListExtendedDto row)
            return false;

        switch (actionKey)
        {
            case "nav:Inventory:WarehouseForm":
            case "form:EditWarehouse":
                InventoryPopupService.ShowEditWarehouse(row.Id);
                return true;
            case "nav:Inventory:Transfers":
                InventoryNavigationContext.BeginCreateTransfer(row.Id);
                NavigationStateManager.Instance.NavigateTo(AppModule.Inventory, "TransferForm");
                return true;
            case "nav:Inventory:Stocktake":
                InventoryNavigationContext.BeginCreateStocktake(row.Id);
                NavigationStateManager.Instance.NavigateTo(AppModule.Inventory, "StocktakeForm");
                return true;
            case "ws:ArchiveWarehouse":
                _ = ArchiveAsync(row);
                return true;
            case "ws:ActivateWarehouse":
                _ = ActivateAsync(row);
                return true;
            default:
                return false;
        }
    }

    public static void OpenOperationsCenter(WarehouseListExtendedDto row, string? tab = null)
    {
        InventoryNavigationContext.BeginWorkspace(row.Id, tab);
        NavigationStateManager.Instance.NavigateTo(AppModule.Inventory, "WarehouseOperationsCenter");
    }

    private static async Task ArchiveAsync(WarehouseListExtendedDto row)
    {
        if (!ConfirmationDialogService.ConfirmDangerous("أرشفة المستودع", row.NameAr))
            return;

        var result = await InventoryUiService.Instance.ArchiveWarehouseAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
            InventoryListRefreshHub.RequestRefresh();
    }

    private static async Task ActivateAsync(WarehouseListExtendedDto row)
    {
        var result = await InventoryUiService.Instance.ActivateWarehouseAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
            InventoryListRefreshHub.RequestRefresh();
    }

    private static async Task DuplicateAsync(WarehouseListExtendedDto row)
    {
        var result = await InventoryUiService.Instance.DuplicateWarehouseAsync(row.Id);
        if (ApplicationResultPresenter.Present(result) && result.IsSuccess)
        {
            InventoryListRefreshHub.RequestRefresh();
            InventoryPopupService.ShowEditWarehouse(result.Value);
        }
    }
}

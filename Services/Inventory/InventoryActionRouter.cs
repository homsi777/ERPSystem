using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Controls.Inventory;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Services;

namespace ERPSystem.Services.Inventory;

/// <summary>
/// توجيه مهام المستودع — كل الإجراءات عبر نوافذ منبثقة فقط (بدون NavigateTo).
/// </summary>
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
                InventoryPopupService.ShowWarehouseWorkspace(row.Id);
                break;
            case EntityActionId.WarehouseEdit:
                InventoryPopupService.ShowEditWarehouse(row.Id);
                break;
            case EntityActionId.WarehouseProperties:
                InventoryPopupService.ShowWarehouseProperties(row.Id);
                break;
            case EntityActionId.FabricTransfer:
                InventoryPopupService.ShowTransferWizard(row.Id);
                break;
            case EntityActionId.WarehouseStockReport:
                InventoryPopupService.ShowWarehousePanel(row.Id, WarehousePopupPanel.Stock);
                break;
            case EntityActionId.FabricMovement:
            case EntityActionId.WarehouseMovementHistory:
                InventoryPopupService.ShowWarehousePanel(row.Id, WarehousePopupPanel.Movements);
                break;
            case EntityActionId.WarehouseStocktake:
                InventoryPopupService.ShowStocktakeWizard(row.Id);
                break;
            case EntityActionId.WarehouseTimeline:
                InventoryPopupService.ShowWarehousePanel(row.Id, WarehousePopupPanel.Timeline);
                break;
            case EntityActionId.WarehouseAudit:
                InventoryPopupService.ShowWarehousePanel(row.Id, WarehousePopupPanel.Audit);
                break;
            case EntityActionId.WarehouseExportExcel:
                InventoryExportService.ExportWarehouseStock(row);
                break;
            case EntityActionId.WarehouseExportPdf:
                InventoryPopupService.ShowWarehousePrintPreview(row);
                break;
            case EntityActionId.WarehousePrint:
                InventoryPopupService.ShowWarehousePrintPreview(row);
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
            case "form:EditWarehouse":
                InventoryPopupService.ShowEditWarehouse(row.Id);
                return true;
            case "nav:Inventory:Transfers":
                InventoryPopupService.ShowTransferWizard(row.Id);
                return true;
            case "nav:Inventory:Stocktake":
                InventoryPopupService.ShowStocktakeWizard(row.Id);
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

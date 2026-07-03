using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Application.Queries.Inventory;
using ERPSystem.Application.Results;

namespace ERPSystem.Application.UseCases.Inventory;

public sealed class GetInventoryWarehouseListHandler(IInventoryManagementRepository repository)
    : IQueryHandler<GetInventoryWarehouseListQuery, ApplicationResult<IReadOnlyList<WarehouseListExtendedDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<WarehouseListExtendedDto>>> HandleAsync(
        GetInventoryWarehouseListQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<IReadOnlyList<WarehouseListExtendedDto>>.Success(
            await repository.GetWarehouseListAsync(query.BranchId, cancellationToken));
}

public sealed class GetInventoryWarehouseDetailHandler(IInventoryManagementRepository repository)
    : IQueryHandler<GetInventoryWarehouseDetailQuery, ApplicationResult<WarehouseDetailDto>>
{
    public async Task<ApplicationResult<WarehouseDetailDto>> HandleAsync(
        GetInventoryWarehouseDetailQuery query, CancellationToken cancellationToken = default)
    {
        var detail = await repository.GetWarehouseDetailAsync(query.WarehouseId, cancellationToken);
        return detail is null
            ? ApplicationResult<WarehouseDetailDto>.NotFound("Warehouse not found.")
            : ApplicationResult<WarehouseDetailDto>.Success(detail);
    }
}

public sealed class GetInventoryOperationsCenterHandler(IInventoryManagementRepository repository)
    : IQueryHandler<GetInventoryWarehouseOperationsCenterQuery, ApplicationResult<InventoryOperationsCenterDto>>
{
    public async Task<ApplicationResult<InventoryOperationsCenterDto>> HandleAsync(
        GetInventoryWarehouseOperationsCenterQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            return ApplicationResult<InventoryOperationsCenterDto>.Success(
                await repository.GetOperationsCenterAsync(query.WarehouseId, cancellationToken));
        }
        catch (InvalidOperationException)
        {
            return ApplicationResult<InventoryOperationsCenterDto>.NotFound("Warehouse not found.");
        }
    }
}

public sealed class GetInventoryDashboardHandler(IInventoryManagementRepository repository)
    : IQueryHandler<GetInventoryDashboardQuery, ApplicationResult<InventoryDashboardDto>>
{
    public async Task<ApplicationResult<InventoryDashboardDto>> HandleAsync(
        GetInventoryDashboardQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<InventoryDashboardDto>.Success(
            await repository.GetDashboardAsync(query.BranchId, cancellationToken));
}

public sealed class GetFabricStockBalancesHandler(IInventoryManagementRepository repository)
    : IQueryHandler<GetFabricStockBalancesQuery, ApplicationResult<IReadOnlyList<FabricStockBalanceDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<FabricStockBalanceDto>>> HandleAsync(
        GetFabricStockBalancesQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<IReadOnlyList<FabricStockBalanceDto>>.Success(
            await repository.GetFabricStockBalancesAsync(query.BranchId, query.WarehouseId, cancellationToken));
}

public sealed class GetInventoryMovementsHandler(IInventoryManagementRepository repository)
    : IQueryHandler<GetInventoryMovementsQuery, ApplicationResult<IReadOnlyList<StockMovementListDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<StockMovementListDto>>> HandleAsync(
        GetInventoryMovementsQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<IReadOnlyList<StockMovementListDto>>.Success(
            await repository.GetMovementsAsync(query.BranchId, query.WarehouseId, cancellationToken));
}

public sealed class GetInventoryAlertsHandler(IInventoryManagementRepository repository)
    : IQueryHandler<GetInventoryAlertsQuery, ApplicationResult<IReadOnlyList<InventoryAlertDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<InventoryAlertDto>>> HandleAsync(
        GetInventoryAlertsQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<IReadOnlyList<InventoryAlertDto>>.Success(
            await repository.GetAlertsAsync(query.BranchId, query.UnacknowledgedOnly, cancellationToken));
}

public sealed class GetStockTransfersHandler(IInventoryManagementRepository repository)
    : IQueryHandler<GetStockTransfersQuery, ApplicationResult<IReadOnlyList<StockTransferListDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<StockTransferListDto>>> HandleAsync(
        GetStockTransfersQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<IReadOnlyList<StockTransferListDto>>.Success(
            await repository.GetTransfersAsync(query.BranchId, cancellationToken));
}

public sealed class GetStockTransferDetailHandler(IInventoryManagementRepository repository)
    : IQueryHandler<GetStockTransferDetailQuery, ApplicationResult<StockTransferDetailDto>>
{
    public async Task<ApplicationResult<StockTransferDetailDto>> HandleAsync(
        GetStockTransferDetailQuery query, CancellationToken cancellationToken = default)
    {
        var detail = await repository.GetTransferDetailAsync(query.TransferId, cancellationToken);
        return detail is null
            ? ApplicationResult<StockTransferDetailDto>.NotFound("Transfer not found.")
            : ApplicationResult<StockTransferDetailDto>.Success(detail);
    }
}

public sealed class GetWarehouseTransferRollsHandler(IInventoryManagementRepository repository)
    : IQueryHandler<GetWarehouseTransferRollsQuery, ApplicationResult<IReadOnlyList<WarehouseTransferRollDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<WarehouseTransferRollDto>>> HandleAsync(
        GetWarehouseTransferRollsQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<IReadOnlyList<WarehouseTransferRollDto>>.Success(
            await repository.GetTransferableRollsAsync(query.WarehouseId, cancellationToken));
}

public sealed class GetStocktakeSessionsHandler(IInventoryManagementRepository repository)
    : IQueryHandler<GetStocktakeSessionsQuery, ApplicationResult<IReadOnlyList<StocktakeListDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<StocktakeListDto>>> HandleAsync(
        GetStocktakeSessionsQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<IReadOnlyList<StocktakeListDto>>.Success(
            await repository.GetStocktakeSessionsAsync(query.BranchId, cancellationToken));
}

public sealed class GetStocktakeDetailHandler(IInventoryManagementRepository repository)
    : IQueryHandler<GetStocktakeDetailQuery, ApplicationResult<StocktakeDetailDto>>
{
    public async Task<ApplicationResult<StocktakeDetailDto>> HandleAsync(
        GetStocktakeDetailQuery query, CancellationToken cancellationToken = default)
    {
        var detail = await repository.GetStocktakeDetailAsync(query.SessionId, cancellationToken);
        return detail is null
            ? ApplicationResult<StocktakeDetailDto>.NotFound("Stocktake session not found.")
            : ApplicationResult<StocktakeDetailDto>.Success(detail);
    }
}

public sealed class GetOpeningStockDocumentsHandler(IInventoryManagementRepository repository)
    : IQueryHandler<GetOpeningStockDocumentsQuery, ApplicationResult<IReadOnlyList<OpeningStockListDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<OpeningStockListDto>>> HandleAsync(
        GetOpeningStockDocumentsQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<IReadOnlyList<OpeningStockListDto>>.Success(
            await repository.GetOpeningStockDocumentsAsync(query.BranchId, cancellationToken));
}

public sealed class GetWarehouseStorageLocationsHandler(IInventoryManagementRepository repository)
    : IQueryHandler<GetWarehouseStorageLocationsQuery, ApplicationResult<IReadOnlyList<StorageLocationDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<StorageLocationDto>>> HandleAsync(
        GetWarehouseStorageLocationsQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<IReadOnlyList<StorageLocationDto>>.Success(
            await repository.GetLocationsAsync(query.WarehouseId, cancellationToken));
}

public sealed class GetInventoryAuditTrailHandler(IInventoryManagementRepository repository)
    : IQueryHandler<GetInventoryAuditTrailQuery, ApplicationResult<IReadOnlyList<InventoryAuditDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<InventoryAuditDto>>> HandleAsync(
        GetInventoryAuditTrailQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<IReadOnlyList<InventoryAuditDto>>.Success(
            await repository.GetAuditTrailAsync(query.EntityId, query.EntityType, cancellationToken));
}

public sealed class GetInventoryTimelineHandler(IInventoryManagementRepository repository)
    : IQueryHandler<GetInventoryTimelineQuery, ApplicationResult<IReadOnlyList<InventoryTimelineDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<InventoryTimelineDto>>> HandleAsync(
        GetInventoryTimelineQuery query, CancellationToken cancellationToken = default) =>
        ApplicationResult<IReadOnlyList<InventoryTimelineDto>>.Success(
            await repository.GetTimelineAsync(query.EntityId, query.EntityType, cancellationToken));
}

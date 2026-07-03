using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Domain.Entities.Inventory;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IInventoryManagementRepository
{
    Task<bool> WarehouseCodeExistsAsync(Guid branchId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddWarehouseAsync(Warehouse warehouse, CancellationToken cancellationToken = default);
    Task UpdateWarehouseAsync(Warehouse warehouse, CancellationToken cancellationToken = default);
    Task<Warehouse?> GetWarehouseByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseListExtendedDto>> GetWarehouseListAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task ArchiveWarehouseAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> WarehouseHasStockAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<WarehouseDetailDto?> GetWarehouseDetailAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddLocationAsync(WarehouseLocation location, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StorageLocationDto>> GetLocationsAsync(Guid warehouseId, CancellationToken cancellationToken = default);

    Task AddAuditEntryAsync(InventoryAuditEntry entry, CancellationToken cancellationToken = default);
    Task AddTimelineEventAsync(InventoryTimelineEvent entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryAuditDto>> GetAuditTrailAsync(Guid entityId, string entityType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryTimelineDto>> GetTimelineAsync(Guid entityId, string entityType, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FabricStockBalanceDto>> GetFabricStockBalancesAsync(Guid branchId, Guid? warehouseId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FabricRollListDto>> GetFabricRollsAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockMovementListDto>> GetMovementsAsync(Guid branchId, Guid? warehouseId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryAlertDto>> GetAlertsAsync(Guid branchId, bool unacknowledgedOnly = true, CancellationToken cancellationToken = default);
    Task<InventoryDashboardDto> GetDashboardAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<InventoryOperationsCenterDto> GetOperationsCenterAsync(Guid warehouseId, CancellationToken cancellationToken = default);

    Task<Guid> CreateTransferAsync(StockTransfer transfer, IReadOnlyList<(Guid FabricItemId, Guid FabricColorId, decimal Meters, int Rolls, Guid? RollId)> lines, CancellationToken cancellationToken = default);
    Task<StockTransferDetailDto?> GetTransferDetailAsync(Guid transferId, CancellationToken cancellationToken = default);
    Task ApproveTransferAsync(Guid transferId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseTransferRollDto>> GetTransferableRollsAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<bool> ValidateRollTransferAsync(Guid rollId, Guid fromWarehouseId, decimal meters, CancellationToken cancellationToken = default);

    Task<Guid> CreateStocktakeAsync(StocktakeSession session, CancellationToken cancellationToken = default);
    Task SeedStocktakeLinesAsync(Guid sessionId, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<StocktakeDetailDto?> GetStocktakeDetailAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task UpdateStocktakeLineCountsAsync(Guid sessionId, IReadOnlyList<(Guid LineId, decimal CountedMeters)> lines, CancellationToken cancellationToken = default);
    Task AddStocktakeLineAsync(Guid sessionId, Guid fabricItemId, Guid fabricColorId, decimal systemMeters, decimal countedMeters, Guid? rollId = null, CancellationToken cancellationToken = default);
    Task<Guid> CreateOpeningStockAsync(OpeningStockDocument doc, IReadOnlyList<(Guid FabricItemId, Guid FabricColorId, decimal Meters, int Rolls, decimal UnitCost)> lines, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockTransferListDto>> GetTransfersAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StocktakeListDto>> GetStocktakeSessionsAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OpeningStockListDto>> GetOpeningStockDocumentsAsync(Guid branchId, CancellationToken cancellationToken = default);
}

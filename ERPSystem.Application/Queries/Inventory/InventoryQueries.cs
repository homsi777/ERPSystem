using ERPSystem.Application.DTOs.Inventory;

namespace ERPSystem.Application.Queries.Inventory;

public sealed record GetInventoryWarehouseListQuery(Guid BranchId);
public sealed record GetInventoryWarehouseOperationsCenterQuery(Guid WarehouseId);
public sealed record GetInventoryWarehouseDetailQuery(Guid WarehouseId);
public sealed record GetInventoryDashboardQuery(Guid BranchId);
public sealed record GetFabricStockBalancesQuery(Guid BranchId, Guid? WarehouseId = null, string? Search = null);
public sealed record GetFabricSearchProfilesQuery(Guid BranchId, string Search, Guid? WarehouseId = null);
public sealed record GetInventoryMovementsQuery(Guid BranchId, Guid? WarehouseId = null);
public sealed record GetInventoryAlertsQuery(Guid BranchId, bool UnacknowledgedOnly = true);
public sealed record GetStockTransfersQuery(Guid BranchId);
public sealed record GetStockTransferDetailQuery(Guid TransferId);
public sealed record GetWarehouseTransferRollsQuery(Guid WarehouseId);
public sealed record GetFabricRollsPageQuery(Guid WarehouseId, int PageNumber = 1, int PageSize = 50, int? Status = null, string? Search = null);
public sealed record GetFabricRollsByStockQuery(Guid WarehouseId, Guid ContainerId, Guid FabricItemId, Guid FabricColorId);
public sealed record GetFabricRollSalesReservationsQuery(IReadOnlyList<Guid> RollIds, Guid? ExcludeSalesInvoiceId = null);
public sealed record GetDetailingCandidateRollsQuery(Guid WarehouseId, Guid ContainerId, Guid FabricItemId, Guid FabricColorId, Guid? ExcludeSalesInvoiceId = null);
public sealed record GetStocktakeSessionsQuery(Guid BranchId);
public sealed record GetStocktakeDetailQuery(Guid SessionId);
public sealed record GetOpeningStockDocumentsQuery(Guid BranchId);
public sealed record GetWarehouseStorageLocationsQuery(Guid WarehouseId);
public sealed record GetInventoryAuditTrailQuery(Guid EntityId, string EntityType);
public sealed record GetInventoryTimelineQuery(Guid EntityId, string EntityType);

public sealed class InventoryListFilter
{
    public Guid? WarehouseId { get; init; }
    public string? Search { get; init; }
}

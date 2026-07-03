namespace ERPSystem.Application.Commands.Inventory;

public sealed record CreateWarehouseCommand(
    Guid BranchId,
    string Code,
    string NameAr,
    string City,
    string? NameEn = null,
    string? Description = null,
    string? Address = null,
    string? Manager = null,
    Guid? CostCenterId = null,
    string? Notes = null,
    bool IsDefault = false,
    int? CapacityRolls = null);

public sealed record UpdateWarehouseCommand(
    Guid WarehouseId,
    string NameAr,
    string City,
    string? NameEn = null,
    string? Description = null,
    string? Address = null,
    string? Manager = null,
    Guid? CostCenterId = null,
    string? Notes = null,
    int? CapacityRolls = null,
    bool? IsDefault = null);

public sealed record DeactivateWarehouseCommand(Guid WarehouseId);
public sealed record ActivateWarehouseCommand(Guid WarehouseId);
public sealed record ArchiveWarehouseCommand(Guid WarehouseId);
public sealed record DuplicateWarehouseCommand(Guid WarehouseId);

public sealed record CreateStorageLocationCommand(
    Guid WarehouseId,
    int LocationType,
    string Code,
    string Name,
    Guid? ParentId = null,
    string Zone = "",
    string BinCode = "",
    decimal? CapacityMeters = null,
    int Priority = 0);

public sealed record CreateStockTransferCommand(
    Guid BranchId,
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    Guid? FromLocationId,
    Guid? ToLocationId,
    string? Notes,
    IReadOnlyList<StockTransferLineCommand> Lines);

public sealed record StockTransferLineCommand(
    Guid FabricItemId,
    Guid FabricColorId,
    decimal QuantityMeters,
    int RollCount,
    Guid? FabricRollId = null);

public sealed record ApproveStockTransferCommand(Guid TransferId);
public sealed record CompleteStockTransferCommand(Guid TransferId);

public sealed record UpdateStocktakeLinesCommand(
    Guid SessionId,
    IReadOnlyList<StocktakeLineCountCommand> Lines);

public sealed record StocktakeLineCountCommand(Guid LineId, decimal CountedMeters);

public sealed record CreateStocktakeCommand(
    Guid BranchId,
    Guid WarehouseId,
    string Responsible,
    Guid? LocationId = null,
    string? Notes = null);

public sealed record PostStocktakeCommand(Guid SessionId);

public sealed record CreateOpeningStockCommand(
    Guid BranchId,
    Guid WarehouseId,
    DateTime OpeningDate,
    string? Reference,
    string CurrencyCode,
    string? Notes,
    IReadOnlyList<OpeningStockLineCommand> Lines);

public sealed record OpeningStockLineCommand(
    Guid FabricItemId,
    Guid FabricColorId,
    decimal QuantityMeters,
    int RollCount,
    decimal UnitCost);

public sealed record PostOpeningStockCommand(Guid DocumentId);

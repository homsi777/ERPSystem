using ERPSystem.Application.DTOs.Inventory;

namespace ERPSystem.Application.Common;

/// <summary>Strips cost/import pricing from DTOs when the caller is not a general manager.</summary>
public static class SensitivePricingSanitizer
{
    public static IReadOnlyList<FabricRollListDto> RedactRolls(IReadOnlyList<FabricRollListDto> items, bool canView) =>
        canView ? items : items.Select(Redact).ToList();

    public static PaginatedFabricRollDto Redact(PaginatedFabricRollDto page, bool canView) =>
        canView
            ? page
            : new PaginatedFabricRollDto
            {
                Items = RedactRolls(page.Items, false),
                TotalCount = page.TotalCount,
                PageNumber = page.PageNumber,
                PageSize = page.PageSize
            };

    public static IReadOnlyList<FabricSearchProfileDto> RedactProfiles(
        IReadOnlyList<FabricSearchProfileDto> items,
        bool canView) =>
        canView ? items : items.Select(Redact).ToList();

    public static IReadOnlyList<FabricStockBalanceDto> RedactStock(
        IReadOnlyList<FabricStockBalanceDto> items,
        bool canView) =>
        canView
            ? items
            : items.Select(s => new FabricStockBalanceDto
            {
                WarehouseId = s.WarehouseId,
                WarehouseName = s.WarehouseName,
                ContainerId = s.ContainerId,
                ContainerNumber = s.ContainerNumber,
                FabricItemId = s.FabricItemId,
                FabricCode = s.FabricCode,
                FabricName = s.FabricName,
                FabricColorId = s.FabricColorId,
                ColorName = s.ColorName,
                RollCount = s.RollCount,
                TotalMeters = s.TotalMeters,
                ReservedMeters = s.ReservedMeters,
                AvailableMeters = s.AvailableMeters,
                InventoryValue = 0
            }).ToList();

    public static InventoryDashboardDto Redact(InventoryDashboardDto dto, bool canView) =>
        canView
            ? dto
            : new InventoryDashboardDto
            {
                TotalInventoryValue = 0,
                WarehouseCount = dto.WarehouseCount,
                TotalRolls = dto.TotalRolls,
                TotalMeters = dto.TotalMeters,
                ReservedMeters = dto.ReservedMeters,
                LowStockCount = dto.LowStockCount,
                PendingTransfers = dto.PendingTransfers,
                PendingStocktakes = dto.PendingStocktakes,
                ActiveAlerts = dto.ActiveAlerts,
                TopFabrics = RedactStock(dto.TopFabrics, false),
                RecentAlerts = dto.RecentAlerts
            };

    public static IReadOnlyList<StockMovementListDto> RedactMovements(
        IReadOnlyList<StockMovementListDto> items,
        bool canView) =>
        canView
            ? items
            : items.Select(m => new StockMovementListDto
            {
                Id = m.Id,
                MovementNumber = m.MovementNumber,
                MovementDate = m.MovementDate,
                Type = m.Type,
                WarehouseName = m.WarehouseName,
                Reference = m.Reference,
                TotalMeters = m.TotalMeters,
                TotalValue = 0,
                Status = m.Status
            }).ToList();

    public static IReadOnlyList<OpeningStockListDto> RedactOpeningStock(
        IReadOnlyList<OpeningStockListDto> items,
        bool canView) =>
        canView
            ? items
            : items.Select(d => new OpeningStockListDto
            {
                Id = d.Id,
                DocumentNumber = d.DocumentNumber,
                WarehouseName = d.WarehouseName,
                OpeningDate = d.OpeningDate,
                Status = d.Status,
                TotalValue = 0
            }).ToList();

    private static FabricRollListDto Redact(FabricRollListDto dto) =>
        new()
        {
            Id = dto.Id,
            RollNumber = dto.RollNumber,
            Barcode = dto.Barcode,
            FabricName = dto.FabricName,
            ColorName = dto.ColorName,
            LengthMeters = dto.LengthMeters,
            RemainingLengthMeters = dto.RemainingLengthMeters,
            CostPerMeter = 0,
            CurrentValue = 0,
            Status = dto.Status,
            BatchNumber = dto.BatchNumber,
            LocationCode = dto.LocationCode,
            LotCode = dto.LotCode
        };

    private static FabricSearchProfileDto Redact(FabricSearchProfileDto dto) =>
        new()
        {
            FabricItemId = dto.FabricItemId,
            FabricCode = dto.FabricCode,
            FabricName = dto.FabricName,
            CategoryName = dto.CategoryName,
            TotalRolls = dto.TotalRolls,
            TotalMeters = dto.TotalMeters,
            AvailableMeters = dto.AvailableMeters,
            ReservedMeters = dto.ReservedMeters,
            InventoryValue = 0,
            AvgCostPerMeter = null,
            AvgSalePricePerMeter = dto.AvgSalePricePerMeter,
            MinSalePricePerMeter = dto.MinSalePricePerMeter,
            MaxSalePricePerMeter = dto.MaxSalePricePerMeter,
            WarehouseCount = dto.WarehouseCount,
            ContainerCount = dto.ContainerCount,
            ColorCount = dto.ColorCount,
            Colors = dto.Colors.Select(Redact).ToList(),
            Locations = dto.Locations.Select(Redact).ToList(),
            ContainerJourney = dto.ContainerJourney.Select(Redact).ToList(),
            JourneyTimeline = dto.JourneyTimeline
        };

    private static FabricSearchColorBreakdownDto Redact(FabricSearchColorBreakdownDto dto) =>
        new()
        {
            FabricColorId = dto.FabricColorId,
            ColorName = dto.ColorName,
            RollCount = dto.RollCount,
            TotalMeters = dto.TotalMeters,
            AvailableMeters = dto.AvailableMeters,
            ReservedMeters = dto.ReservedMeters,
            InventoryValue = 0,
            AvgSalePricePerMeter = dto.AvgSalePricePerMeter,
            AvgCostPerMeter = null,
            ContainerCount = dto.ContainerCount
        };

    private static FabricSearchLocationDetailDto Redact(FabricSearchLocationDetailDto dto) =>
        new()
        {
            WarehouseId = dto.WarehouseId,
            WarehouseName = dto.WarehouseName,
            ContainerId = dto.ContainerId,
            ContainerNumber = dto.ContainerNumber,
            FabricColorId = dto.FabricColorId,
            ColorName = dto.ColorName,
            RollCount = dto.RollCount,
            TotalMeters = dto.TotalMeters,
            AvailableMeters = dto.AvailableMeters,
            ReservedMeters = dto.ReservedMeters,
            InventoryValue = 0,
            AvgCostPerMeter = null,
            AvgSalePricePerMeter = dto.AvgSalePricePerMeter
        };

    private static FabricSearchContainerLegDto Redact(FabricSearchContainerLegDto dto) =>
        new()
        {
            ContainerId = dto.ContainerId,
            ContainerNumber = dto.ContainerNumber,
            StatusLabel = dto.StatusLabel,
            SupplierName = dto.SupplierName,
            ShipmentDate = dto.ShipmentDate,
            ArrivalDate = dto.ArrivalDate,
            ApprovedAt = dto.ApprovedAt,
            RollCount = dto.RollCount,
            TotalMeters = dto.TotalMeters,
            LandedCostPerMeter = null,
            SalePricePerMeter = dto.SalePricePerMeter,
            Warehouses = dto.Warehouses
        };
}

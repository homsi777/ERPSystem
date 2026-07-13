using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Persistence.Models.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERPSystem.Infrastructure.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovementEntity>
{
    public void Configure(EntityTypeBuilder<StockMovementEntity> builder)
    {
        builder.ToTable("StockMovements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MovementNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.MovementNumber).IsUnique();
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId, x.Type });
        builder.HasIndex(x => new { x.WarehouseId, x.MovementDate })
            .HasDatabaseName("idx_stock_movements_warehouse_date");
        builder.HasIndex(x => x.MovementDate)
            .HasDatabaseName("idx_stock_movements_date");
    }
}

internal sealed class StockMovementLineConfiguration : IEntityTypeConfiguration<StockMovementLineEntity>
{
    public void Configure(EntityTypeBuilder<StockMovementLineEntity> builder)
    {
        builder.ToTable("stock_movement_lines", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QuantityMeters).HasPrecision(18, 4);
        builder.Property(x => x.UnitCost).HasPrecision(18, 4);
        builder.Property(x => x.TotalValue).HasPrecision(18, 4);
        builder.HasIndex(x => x.MovementId);
        builder.HasIndex(x => x.FabricRollId);
        builder.HasIndex(x => new { x.MovementId, x.FabricItemId, x.FabricRollId })
            .HasDatabaseName("idx_stock_movement_lines_movement_fabric");
    }
}

internal sealed class WarehouseLocationConfiguration : IEntityTypeConfiguration<WarehouseLocationEntity>
{
    public void Configure(EntityTypeBuilder<WarehouseLocationEntity> builder)
    {
        builder.ToTable("warehouse_locations", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Zone).HasMaxLength(100);
        builder.Property(x => x.BinCode).HasMaxLength(50);
        builder.HasIndex(x => new { x.WarehouseId, x.Code }).IsUnique();
    }
}

internal sealed class FabricBatchConfiguration : IEntityTypeConfiguration<FabricBatchEntity>
{
    public void Configure(EntityTypeBuilder<FabricBatchEntity> builder)
    {
        builder.ToTable("fabric_batches", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BatchNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.BatchNumber).IsUnique();
        builder.Property(x => x.LandingCostPerMeter).HasPrecision(18, 4);
        builder.Property(x => x.TotalMeters).HasPrecision(18, 4);
    }
}

internal sealed class FabricRollConfiguration : IEntityTypeConfiguration<FabricRollEntity>
{
    public void Configure(EntityTypeBuilder<FabricRollEntity> builder)
    {
        builder.ToTable("FabricRolls");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LengthMeters).HasPrecision(18, 4);
        builder.Property(x => x.RemainingLengthMeters).HasPrecision(18, 4);
        builder.Property(x => x.CostPerMeter).HasPrecision(18, 4);
        builder.HasIndex(x => x.Barcode);
        builder.HasIndex(x => x.FabricBatchId);
        builder.HasIndex(x => new { x.WarehouseId, x.Status, x.RemainingLengthMeters, x.RollNumber })
            .HasDatabaseName("idx_fabric_rolls_warehouse_status");
        builder.HasIndex(x => new { x.WarehouseId, x.FabricColorId, x.RemainingLengthMeters })
            .HasDatabaseName("idx_fabric_rolls_warehouse_color");
        builder.HasIndex(x => new { x.ContainerId, x.Status })
            .HasDatabaseName("idx_fabric_rolls_container");
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("idx_fabric_rolls_status");
        builder.HasIndex(x => new { x.IsLegacyOpeningBalance, x.WarehouseId, x.Status })
            .HasDatabaseName("idx_fabric_rolls_legacy_opening");
        builder.HasIndex(x => new { x.WarehouseId, x.Status, x.RollNumber })
            .HasDatabaseName("idx_fabric_rolls_available_partial")
            .HasFilter("\"Status\" = 0 AND \"RemainingLengthMeters\" > 0");
    }
}

internal sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservationEntity>
{
    public void Configure(EntityTypeBuilder<InventoryReservationEntity> builder)
    {
        builder.ToTable("inventory_reservations", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReservedMeters).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId });
    }
}

internal sealed class StockTransferDocumentConfiguration : IEntityTypeConfiguration<StockTransferDocumentEntity>
{
    public void Configure(EntityTypeBuilder<StockTransferDocumentEntity> builder)
    {
        builder.ToTable("stock_transfers", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Number).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Number).IsUnique();
    }
}

internal sealed class StockTransferLineConfiguration : IEntityTypeConfiguration<StockTransferLineEntity>
{
    public void Configure(EntityTypeBuilder<StockTransferLineEntity> builder)
    {
        builder.ToTable("stock_transfer_lines", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QuantityMeters).HasPrecision(18, 4);
    }
}

internal sealed class StocktakeSessionConfiguration : IEntityTypeConfiguration<StocktakeSessionEntity>
{
    public void Configure(EntityTypeBuilder<StocktakeSessionEntity> builder)
    {
        builder.ToTable("stocktake_sessions", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SessionNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.SessionNumber).IsUnique();
    }
}

internal sealed class StocktakeLineConfiguration : IEntityTypeConfiguration<StocktakeLineEntity>
{
    public void Configure(EntityTypeBuilder<StocktakeLineEntity> builder)
    {
        builder.ToTable("stocktake_lines", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SystemMeters).HasPrecision(18, 4);
        builder.Property(x => x.CountedMeters).HasPrecision(18, 4);
        builder.Property(x => x.DifferenceMeters).HasPrecision(18, 4);
    }
}

internal sealed class OpeningStockDocumentConfiguration : IEntityTypeConfiguration<OpeningStockDocumentEntity>
{
    public void Configure(EntityTypeBuilder<OpeningStockDocumentEntity> builder)
    {
        builder.ToTable("opening_stock_documents", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.DocumentNumber).IsUnique();
    }
}

internal sealed class OpeningStockLineConfiguration : IEntityTypeConfiguration<OpeningStockLineEntity>
{
    public void Configure(EntityTypeBuilder<OpeningStockLineEntity> builder)
    {
        builder.ToTable("opening_stock_lines", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QuantityMeters).HasPrecision(18, 4);
        builder.Property(x => x.UnitCost).HasPrecision(18, 4);
        builder.Property(x => x.TotalValue).HasPrecision(18, 4);
    }
}

internal sealed class InventoryRuleConfiguration : IEntityTypeConfiguration<InventoryRuleEntity>
{
    public void Configure(EntityTypeBuilder<InventoryRuleEntity> builder)
    {
        builder.ToTable("inventory_rules", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MinimumStock).HasPrecision(18, 4);
        builder.Property(x => x.MaximumStock).HasPrecision(18, 4);
        builder.Property(x => x.SafetyStock).HasPrecision(18, 4);
        builder.Property(x => x.ReorderPoint).HasPrecision(18, 4);
    }
}

internal sealed class InventoryAlertConfiguration : IEntityTypeConfiguration<InventoryAlertEntity>
{
    public void Configure(EntityTypeBuilder<InventoryAlertEntity> builder)
    {
        builder.ToTable("inventory_alerts", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.BranchId, x.IsAcknowledged });
    }
}

internal sealed class InventoryAuditEntryConfiguration : IEntityTypeConfiguration<InventoryAuditEntryEntity>
{
    public void Configure(EntityTypeBuilder<InventoryAuditEntryEntity> builder)
    {
        builder.ToTable("inventory_audit_logs", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.EntityId, x.EntityType });
    }
}

internal sealed class InventoryTimelineEventConfiguration : IEntityTypeConfiguration<InventoryTimelineEventEntity>
{
    public void Configure(EntityTypeBuilder<InventoryTimelineEventEntity> builder)
    {
        builder.ToTable("inventory_timeline_events", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.EntityId, x.EntityType });
    }
}

internal sealed class InventoryValuationSnapshotConfiguration : IEntityTypeConfiguration<InventoryValuationSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<InventoryValuationSnapshotEntity> builder)
    {
        builder.ToTable("inventory_valuation_snapshots", Schemas.Inventory);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QuantityMeters).HasPrecision(18, 4);
        builder.Property(x => x.UnitCost).HasPrecision(18, 4);
        builder.Property(x => x.TotalValue).HasPrecision(18, 4);
        builder.HasIndex(x => x.WarehouseId);
    }
}

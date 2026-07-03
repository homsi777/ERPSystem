using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddInventoryEngineModule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE inventory.warehouses ADD COLUMN IF NOT EXISTS "NameEn" character varying(200);
            ALTER TABLE inventory.warehouses ADD COLUMN IF NOT EXISTS "Description" character varying(500);
            ALTER TABLE inventory.warehouses ADD COLUMN IF NOT EXISTS "Address" character varying(500);
            ALTER TABLE inventory.warehouses ADD COLUMN IF NOT EXISTS "Manager" character varying(200);
            ALTER TABLE inventory.warehouses ADD COLUMN IF NOT EXISTS "CostCenterId" uuid;
            ALTER TABLE inventory.warehouses ADD COLUMN IF NOT EXISTS "Notes" character varying(1000);
            ALTER TABLE inventory.warehouses ADD COLUMN IF NOT EXISTS "IsDefault" boolean NOT NULL DEFAULT FALSE;

            CREATE TABLE IF NOT EXISTS inventory.warehouse_locations (
                "Id" uuid NOT NULL,
                "WarehouseId" uuid NOT NULL,
                "ParentId" uuid NULL,
                "LocationType" integer NOT NULL DEFAULT 0,
                "Code" character varying(50) NOT NULL DEFAULT '',
                "Name" text NOT NULL DEFAULT '',
                "Zone" text NOT NULL DEFAULT '',
                "BinCode" text NOT NULL DEFAULT '',
                "CapacityMeters" numeric(18,4) NULL,
                "Status" integer NOT NULL DEFAULT 0,
                "Priority" integer NOT NULL DEFAULT 0,
                "Barcode" text NULL,
                "QrCode" text NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "CreatedByUserId" uuid NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedByUserId" uuid NULL,
                "IsActive" boolean NOT NULL DEFAULT TRUE,
                "IsArchived" boolean NOT NULL DEFAULT FALSE,
                CONSTRAINT "PK_warehouse_locations" PRIMARY KEY ("Id")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_warehouse_locations_WarehouseId_Code"
                ON inventory.warehouse_locations ("WarehouseId", "Code");

            ALTER TABLE "StockMovements" ADD COLUMN IF NOT EXISTS "SourceWarehouseId" uuid;
            ALTER TABLE "StockMovements" ADD COLUMN IF NOT EXISTS "DestinationWarehouseId" uuid;
            ALTER TABLE "StockMovements" ADD COLUMN IF NOT EXISTS "SourceLocationId" uuid;
            ALTER TABLE "StockMovements" ADD COLUMN IF NOT EXISTS "DestinationLocationId" uuid;
            ALTER TABLE "StockMovements" ADD COLUMN IF NOT EXISTS "Reason" text;
            ALTER TABLE "StockMovements" ADD COLUMN IF NOT EXISTS "UserId" uuid;

            ALTER TABLE "FabricRolls" ADD COLUMN IF NOT EXISTS "FabricBatchId" uuid;
            ALTER TABLE "FabricRolls" ADD COLUMN IF NOT EXISTS "StorageLocationId" uuid;
            ALTER TABLE "FabricRolls" ADD COLUMN IF NOT EXISTS "Barcode" text;
            ALTER TABLE "FabricRolls" ADD COLUMN IF NOT EXISTS "QrCode" text;
            ALTER TABLE "FabricRolls" ADD COLUMN IF NOT EXISTS "QualityStatus" integer NOT NULL DEFAULT 0;
            ALTER TABLE "FabricRolls" ADD COLUMN IF NOT EXISTS "ReservationStatus" integer NOT NULL DEFAULT 0;
            """);

        migrationBuilder.CreateTable(
            name: "stock_movement_lines", schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MovementId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricItemId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricColorId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricRollId = table.Column<Guid>(type: "uuid", nullable: true),
                FabricBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                ContainerId = table.Column<Guid>(type: "uuid", nullable: false),
                RollCount = table.Column<int>(type: "integer", nullable: false),
                QuantityMeters = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                UnitCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                TotalValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                CurrencyCode = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_stock_movement_lines", x => x.Id));

        migrationBuilder.CreateTable(
            name: "fabric_batches", schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BatchNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                ContainerId = table.Column<Guid>(type: "uuid", nullable: true),
                PurchaseInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                ArrivalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LandingCostPerMeter = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                CurrencyCode = table.Column<string>(type: "text", nullable: false),
                TotalMeters = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                RollCount = table.Column<int>(type: "integer", nullable: false),
                WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                StorageLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                QualityStatus = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_fabric_batches", x => x.Id));

        migrationBuilder.CreateTable(
            name: "inventory_reservations", schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricRollId = table.Column<Guid>(type: "uuid", nullable: true),
                FabricBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                FabricItemId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricColorId = table.Column<Guid>(type: "uuid", nullable: false),
                ReservedMeters = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                RollCount = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                Strategy = table.Column<int>(type: "integer", nullable: false),
                ReferenceType = table.Column<int>(type: "integer", nullable: false),
                ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                ReferenceLineId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_inventory_reservations", x => x.Id));

        migrationBuilder.CreateTable(
            name: "stock_transfers", schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                FromWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                ToWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                FromLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                ToLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Notes = table.Column<string>(type: "text", nullable: true),
                ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                CancelReason = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_stock_transfers", x => x.Id));

        migrationBuilder.CreateTable(
            name: "stock_transfer_lines", schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TransferId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricItemId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricColorId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricRollId = table.Column<Guid>(type: "uuid", nullable: true),
                FabricBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                QuantityMeters = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                RollCount = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_stock_transfer_lines", x => x.Id));

        migrationBuilder.CreateTable(
            name: "stocktake_sessions", schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SessionNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                Responsible = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                Notes = table.Column<string>(type: "text", nullable: true),
                PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                CancelReason = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_stocktake_sessions", x => x.Id));

        migrationBuilder.CreateTable(
            name: "stocktake_lines", schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricItemId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricColorId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricRollId = table.Column<Guid>(type: "uuid", nullable: true),
                SystemMeters = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                CountedMeters = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                DifferenceMeters = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_stocktake_lines", x => x.Id));

        migrationBuilder.CreateTable(
            name: "opening_stock_documents", schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DocumentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                OpeningDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Reference = table.Column<string>(type: "text", nullable: true),
                CurrencyCode = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                Notes = table.Column<string>(type: "text", nullable: true),
                PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                CancelReason = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_opening_stock_documents", x => x.Id));

        migrationBuilder.CreateTable(
            name: "opening_stock_lines", schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricItemId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricColorId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricRollId = table.Column<Guid>(type: "uuid", nullable: true),
                FabricBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                StorageLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                QuantityMeters = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                RollCount = table.Column<int>(type: "integer", nullable: false),
                UnitCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                TotalValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_opening_stock_lines", x => x.Id));

        migrationBuilder.CreateTable(
            name: "inventory_rules", schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricItemId = table.Column<Guid>(type: "uuid", nullable: true),
                WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                MinimumStock = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                MaximumStock = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                SafetyStock = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                ReorderPoint = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                PreferredWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                PreferredLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                PreferredBatchStrategy = table.Column<int>(type: "integer", nullable: false),
                LeadTimeDays = table.Column<int>(type: "integer", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_inventory_rules", x => x.Id));

        migrationBuilder.CreateTable(
            name: "inventory_alerts", schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                AlertType = table.Column<int>(type: "integer", nullable: false),
                Severity = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "text", nullable: false),
                Message = table.Column<string>(type: "text", nullable: false),
                WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                FabricItemId = table.Column<Guid>(type: "uuid", nullable: true),
                FabricRollId = table.Column<Guid>(type: "uuid", nullable: true),
                FabricBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                IsAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_inventory_alerts", x => x.Id));

        migrationBuilder.CreateTable(
            name: "inventory_audit_logs", schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                EntityType = table.Column<string>(type: "text", nullable: false),
                Action = table.Column<string>(type: "text", nullable: false),
                Username = table.Column<string>(type: "text", nullable: false),
                FieldName = table.Column<string>(type: "text", nullable: true),
                PreviousValue = table.Column<string>(type: "text", nullable: true),
                NewValue = table.Column<string>(type: "text", nullable: true),
                Reason = table.Column<string>(type: "text", nullable: true),
                SourceModule = table.Column<string>(type: "text", nullable: true),
                ReferenceDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_inventory_audit_logs", x => x.Id));

        migrationBuilder.CreateTable(
            name: "inventory_timeline_events", schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                EntityType = table.Column<string>(type: "text", nullable: false),
                EventType = table.Column<string>(type: "text", nullable: false),
                Title = table.Column<string>(type: "text", nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                Username = table.Column<string>(type: "text", nullable: false),
                PreviousValue = table.Column<string>(type: "text", nullable: true),
                NewValue = table.Column<string>(type: "text", nullable: true),
                Reason = table.Column<string>(type: "text", nullable: true),
                OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_inventory_timeline_events", x => x.Id));

        migrationBuilder.CreateTable(
            name: "inventory_valuation_snapshots", schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                FabricItemId = table.Column<Guid>(type: "uuid", nullable: true),
                FabricColorId = table.Column<Guid>(type: "uuid", nullable: true),
                ContainerId = table.Column<Guid>(type: "uuid", nullable: true),
                Method = table.Column<int>(type: "integer", nullable: false),
                QuantityMeters = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                UnitCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                TotalValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                CurrencyCode = table.Column<string>(type: "text", nullable: false),
                SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                MovementId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_inventory_valuation_snapshots", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_fabric_batches_BatchNumber", schema: "inventory", table: "fabric_batches", column: "BatchNumber", unique: true);
        migrationBuilder.CreateIndex(name: "IX_stock_movement_lines_MovementId", schema: "inventory", table: "stock_movement_lines", column: "MovementId");
        migrationBuilder.CreateIndex(name: "IX_stock_transfers_Number", schema: "inventory", table: "stock_transfers", column: "Number", unique: true);
        migrationBuilder.CreateIndex(name: "IX_stocktake_sessions_SessionNumber", schema: "inventory", table: "stocktake_sessions", column: "SessionNumber", unique: true);
        migrationBuilder.CreateIndex(name: "IX_opening_stock_documents_DocumentNumber", schema: "inventory", table: "opening_stock_documents", column: "DocumentNumber", unique: true);
        migrationBuilder.CreateIndex(name: "IX_inventory_alerts_BranchId_IsAcknowledged", schema: "inventory", table: "inventory_alerts", columns: new[] { "BranchId", "IsAcknowledged" });
        migrationBuilder.CreateIndex(name: "IX_inventory_audit_logs_EntityId_EntityType", schema: "inventory", table: "inventory_audit_logs", columns: new[] { "EntityId", "EntityType" });
        migrationBuilder.CreateIndex(name: "IX_inventory_timeline_events_EntityId_EntityType", schema: "inventory", table: "inventory_timeline_events", columns: new[] { "EntityId", "EntityType" });
        migrationBuilder.CreateIndex(name: "IX_FabricRolls_FabricBatchId", table: "FabricRolls", column: "FabricBatchId");
        migrationBuilder.CreateIndex(name: "IX_FabricRolls_Barcode", table: "FabricRolls", column: "Barcode");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "inventory_valuation_snapshots", schema: "inventory");
        migrationBuilder.DropTable(name: "inventory_timeline_events", schema: "inventory");
        migrationBuilder.DropTable(name: "inventory_audit_logs", schema: "inventory");
        migrationBuilder.DropTable(name: "inventory_alerts", schema: "inventory");
        migrationBuilder.DropTable(name: "inventory_rules", schema: "inventory");
        migrationBuilder.DropTable(name: "opening_stock_lines", schema: "inventory");
        migrationBuilder.DropTable(name: "opening_stock_documents", schema: "inventory");
        migrationBuilder.DropTable(name: "stocktake_lines", schema: "inventory");
        migrationBuilder.DropTable(name: "stocktake_sessions", schema: "inventory");
        migrationBuilder.DropTable(name: "stock_transfer_lines", schema: "inventory");
        migrationBuilder.DropTable(name: "stock_transfers", schema: "inventory");
        migrationBuilder.DropTable(name: "inventory_reservations", schema: "inventory");
        migrationBuilder.DropTable(name: "fabric_batches", schema: "inventory");
        migrationBuilder.DropTable(name: "stock_movement_lines", schema: "inventory");

        migrationBuilder.DropColumn(name: "ReservationStatus", table: "FabricRolls");
        migrationBuilder.DropColumn(name: "QualityStatus", table: "FabricRolls");
        migrationBuilder.DropColumn(name: "QrCode", table: "FabricRolls");
        migrationBuilder.DropColumn(name: "Barcode", table: "FabricRolls");
        migrationBuilder.DropColumn(name: "StorageLocationId", table: "FabricRolls");
        migrationBuilder.DropColumn(name: "FabricBatchId", table: "FabricRolls");

        migrationBuilder.DropColumn(name: "UserId", table: "StockMovements");
        migrationBuilder.DropColumn(name: "Reason", table: "StockMovements");
        migrationBuilder.DropColumn(name: "DestinationLocationId", table: "StockMovements");
        migrationBuilder.DropColumn(name: "SourceLocationId", table: "StockMovements");
        migrationBuilder.DropColumn(name: "DestinationWarehouseId", table: "StockMovements");
        migrationBuilder.DropColumn(name: "SourceWarehouseId", table: "StockMovements");

        migrationBuilder.DropColumn(name: "QrCode", schema: "inventory", table: "warehouse_locations");
        migrationBuilder.DropColumn(name: "Barcode", schema: "inventory", table: "warehouse_locations");
        migrationBuilder.DropColumn(name: "Priority", schema: "inventory", table: "warehouse_locations");
        migrationBuilder.DropColumn(name: "Status", schema: "inventory", table: "warehouse_locations");
        migrationBuilder.DropColumn(name: "CapacityMeters", schema: "inventory", table: "warehouse_locations");
        migrationBuilder.DropColumn(name: "Name", schema: "inventory", table: "warehouse_locations");
        migrationBuilder.DropColumn(name: "Code", schema: "inventory", table: "warehouse_locations");
        migrationBuilder.DropColumn(name: "LocationType", schema: "inventory", table: "warehouse_locations");
        migrationBuilder.DropColumn(name: "ParentId", schema: "inventory", table: "warehouse_locations");

        migrationBuilder.DropColumn(name: "IsDefault", schema: "inventory", table: "warehouses");
        migrationBuilder.DropColumn(name: "Notes", schema: "inventory", table: "warehouses");
        migrationBuilder.DropColumn(name: "CostCenterId", schema: "inventory", table: "warehouses");
        migrationBuilder.DropColumn(name: "Manager", schema: "inventory", table: "warehouses");
        migrationBuilder.DropColumn(name: "Address", schema: "inventory", table: "warehouses");
        migrationBuilder.DropColumn(name: "Description", schema: "inventory", table: "warehouses");
        migrationBuilder.DropColumn(name: "NameEn", schema: "inventory", table: "warehouses");
    }
}

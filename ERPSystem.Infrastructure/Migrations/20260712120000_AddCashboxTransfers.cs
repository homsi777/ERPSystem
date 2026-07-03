using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddCashboxTransfers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS finance.cashbox_transfers (
                "Id" uuid NOT NULL,
                "CompanyId" uuid NOT NULL,
                "BranchId" uuid NOT NULL,
                "TransferNumber" character varying(50) NOT NULL,
                "FromCashboxId" uuid NOT NULL,
                "ToCashboxId" uuid NOT NULL,
                "Amount" numeric(18,2) NOT NULL DEFAULT 0,
                "Currency" character varying(10) NOT NULL DEFAULT 'USD',
                "TransferDate" timestamp with time zone NOT NULL,
                "Status" integer NOT NULL DEFAULT 0,
                "Notes" text NULL,
                "PostedAt" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "CreatedByUserId" uuid NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedByUserId" uuid NULL,
                "IsActive" boolean NOT NULL DEFAULT TRUE,
                "IsArchived" boolean NOT NULL DEFAULT FALSE,
                CONSTRAINT "PK_cashbox_transfers" PRIMARY KEY ("Id")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_cashbox_transfers_BranchId_TransferNumber"
                ON finance.cashbox_transfers ("BranchId", "TransferNumber");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS finance.cashbox_transfers;");
    }
}

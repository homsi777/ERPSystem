using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddOpeningBalancesModule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS finance;");

        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS finance.opening_balance_documents (
                "Id" uuid NOT NULL PRIMARY KEY,
                "CompanyId" uuid NOT NULL,
                "BranchId" uuid NOT NULL,
                "Number" character varying(40) NOT NULL,
                "Type" integer NOT NULL,
                "Status" integer NOT NULL,
                "Source" integer NOT NULL,
                "OpeningDate" timestamp with time zone NOT NULL,
                "CurrencyCode" character varying(8) NOT NULL DEFAULT 'USD',
                "ExchangeRate" numeric(18,6) NOT NULL DEFAULT 1,
                "Reference" character varying(200) NULL,
                "Description" character varying(500) NULL,
                "Notes" character varying(2000) NULL,
                "TotalDebit" numeric(18,4) NOT NULL DEFAULT 0,
                "TotalCredit" numeric(18,4) NOT NULL DEFAULT 0,
                "TotalBaseAmount" numeric(18,4) NOT NULL DEFAULT 0,
                "JournalEntryNumber" character varying(40) NULL,
                "ApprovedAt" timestamp with time zone NULL,
                "ApprovedByUserId" uuid NULL,
                "ApprovalNotes" character varying(1000) NULL,
                "RejectionReason" character varying(1000) NULL,
                "PostedAt" timestamp with time zone NULL,
                "PostedByUserId" uuid NULL,
                "LockedAt" timestamp with time zone NULL,
                "ArchivedAt" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "CreatedByUserId" uuid NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedByUserId" uuid NULL,
                "IsActive" boolean NOT NULL DEFAULT TRUE,
                "IsArchived" boolean NOT NULL DEFAULT FALSE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_opening_balance_documents_CompanyId_Number"
                ON finance.opening_balance_documents ("CompanyId", "Number");
            CREATE INDEX IF NOT EXISTS "IX_opening_balance_documents_CompanyId_Type_Status"
                ON finance.opening_balance_documents ("CompanyId", "Type", "Status");
            CREATE INDEX IF NOT EXISTS "IX_opening_balance_documents_OpeningDate"
                ON finance.opening_balance_documents ("OpeningDate");
            """);

        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS finance.opening_balance_lines (
                "Id" uuid NOT NULL PRIMARY KEY,
                "DocumentId" uuid NOT NULL REFERENCES finance.opening_balance_documents("Id") ON DELETE CASCADE,
                "LineNumber" integer NOT NULL,
                "PartyId" uuid NULL,
                "PartyName" character varying(300) NULL,
                "AccountId" uuid NULL,
                "AccountName" character varying(300) NULL,
                "WarehouseId" uuid NULL,
                "WarehouseName" character varying(200) NULL,
                "ItemName" character varying(200) NULL,
                "ColorName" character varying(100) NULL,
                "BatchNumber" character varying(100) NULL,
                "LocationCode" character varying(50) NULL,
                "RollCount" numeric(18,4) NULL,
                "Quantity" numeric(18,4) NULL,
                "UnitCost" numeric(18,4) NULL,
                "BankName" character varying(200) NULL,
                "BankAccountNumber" character varying(50) NULL,
                "InvestmentScope" character varying(200) NULL,
                "Debit" numeric(18,4) NOT NULL DEFAULT 0,
                "Credit" numeric(18,4) NOT NULL DEFAULT 0,
                "Reference" character varying(200) NULL,
                "Description" character varying(500) NULL,
                "Notes" character varying(1000) NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_opening_balance_lines_DocumentId_LineNumber"
                ON finance.opening_balance_lines ("DocumentId", "LineNumber");
            CREATE INDEX IF NOT EXISTS "IX_opening_balance_lines_PartyId"
                ON finance.opening_balance_lines ("PartyId");
            CREATE INDEX IF NOT EXISTS "IX_opening_balance_lines_AccountId"
                ON finance.opening_balance_lines ("AccountId");
            CREATE INDEX IF NOT EXISTS "IX_opening_balance_lines_WarehouseId"
                ON finance.opening_balance_lines ("WarehouseId");
            """);

        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS finance.opening_balance_events (
                "Id" uuid NOT NULL PRIMARY KEY,
                "DocumentId" uuid NOT NULL REFERENCES finance.opening_balance_documents("Id") ON DELETE CASCADE,
                "OccurredAt" timestamp with time zone NOT NULL,
                "UserId" uuid NULL,
                "UserName" character varying(120) NOT NULL,
                "Action" character varying(80) NOT NULL,
                "OldValues" text NULL,
                "NewValues" text NULL,
                "Notes" text NULL,
                "MachineName" character varying(120) NULL,
                "IpAddress" character varying(64) NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_opening_balance_events_DocumentId_OccurredAt"
                ON finance.opening_balance_events ("DocumentId", "OccurredAt");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS finance.opening_balance_events;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS finance.opening_balance_lines;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS finance.opening_balance_documents;");
    }
}

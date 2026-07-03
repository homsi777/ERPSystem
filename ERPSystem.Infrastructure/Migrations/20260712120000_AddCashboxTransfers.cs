using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddCashboxTransfers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS finance.cashbox_transfers (
                id uuid PRIMARY KEY,
                company_id uuid NOT NULL,
                branch_id uuid NOT NULL,
                transfer_number varchar(50) NOT NULL,
                from_cashbox_id uuid NOT NULL,
                to_cashbox_id uuid NOT NULL,
                amount numeric(18,2) NOT NULL DEFAULT 0,
                currency varchar(10) NOT NULL DEFAULT 'USD',
                transfer_date timestamptz NOT NULL,
                status int NOT NULL DEFAULT 0,
                notes text NULL,
                posted_at timestamptz NULL,
                created_at timestamptz NOT NULL DEFAULT now(),
                created_by_user_id uuid NULL,
                updated_at timestamptz NULL,
                updated_by_user_id uuid NULL,
                is_active boolean NOT NULL DEFAULT true,
                is_archived boolean NOT NULL DEFAULT false
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_cashbox_transfers_branch_number
                ON finance.cashbox_transfers (branch_id, transfer_number);
            """);

        migrationBuilder.Sql("""
            INSERT INTO documents.document_counters (id, branch_id, document_type, prefix, last_number, created_at, is_active, is_archived)
            SELECT gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'CashboxTransfer', 'CBT', 0, now(), true, false
            WHERE NOT EXISTS (
                SELECT 1 FROM documents.document_counters
                WHERE branch_id = '22222222-2222-2222-2222-222222222222' AND document_type = 'CashboxTransfer');
            INSERT INTO documents.document_counters (id, branch_id, document_type, prefix, last_number, created_at, is_active, is_archived)
            SELECT gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'Cashbox', 'CASH', 0, now(), true, false
            WHERE NOT EXISTS (
                SELECT 1 FROM documents.document_counters
                WHERE branch_id = '22222222-2222-2222-2222-222222222222' AND document_type = 'Cashbox');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS finance.cashbox_transfers;");
    }
}

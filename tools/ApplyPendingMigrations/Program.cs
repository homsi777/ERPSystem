using Microsoft.Extensions.Configuration;
using Npgsql;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var configuration = new ConfigurationBuilder()
    .SetBasePath(repoRoot)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is missing.");

await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

await ExecuteAsync(conn, """
    ALTER TABLE parties.customers
        ADD COLUMN IF NOT EXISTS "LastReconciliationDate" timestamp with time zone;
    ALTER TABLE parties.customers
        ADD COLUMN IF NOT EXISTS "LastReconciliationBalance" numeric(18,2);
    ALTER TABLE parties.customers
        ADD COLUMN IF NOT EXISTS "LastReconciliationDocumentId" uuid;
    ALTER TABLE sales.sales_invoice_items
        ADD COLUMN IF NOT EXISTS "Notes" character varying(500);
    ALTER TABLE sales.sales_invoice_items
        ADD COLUMN IF NOT EXISTS "OriginalUnitPrice" numeric(18,2) NOT NULL DEFAULT 0;
    ALTER TABLE sales.sales_invoice_items
        ADD COLUMN IF NOT EXISTS "DiscountAmount" numeric(18,2) NOT NULL DEFAULT 0;
    ALTER TABLE sales.sales_invoice_items
        ADD COLUMN IF NOT EXISTS "DiscountReason" character varying(300);
    ALTER TABLE sales.sales_invoice_items
        ADD COLUMN IF NOT EXISTS "PriceModifiedByUserId" uuid;
    ALTER TABLE sales.sales_invoice_items
        ADD COLUMN IF NOT EXISTS "PriceModifiedAt" timestamp with time zone;
    UPDATE sales.sales_invoice_items
        SET "OriginalUnitPrice" = "UnitPrice"
        WHERE "OriginalUnitPrice" = 0;
    ALTER TABLE china_import.container_items
        ADD COLUMN IF NOT EXISTS "SupplierRollNumber" integer;
    """);

Console.WriteLine("Schema columns ensured.");

var historyExists = await ScalarBoolAsync(conn,
    "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'settings' AND table_name = '__ef_migrations_history');");

if (!historyExists)
{
    Console.WriteLine("Skipping migration history — settings.__ef_migrations_history not found.");
    return;
}

foreach (var migrationId in new[]
         {
             "20260715120200_AddSalesInvoiceItemNotes",
             "20260715120300_AddCustomerReconciliationFields",
             "20260715120400_AddSalesInvoiceItemPriceOverride",
             "20260716090000_AddContainerItemSupplierRollNumber"
         })
{
    await using var cmd = new NpgsqlCommand("""
        INSERT INTO settings."__ef_migrations_history" ("MigrationId", "ProductVersion")
        VALUES (@id, '9.0.6')
        ON CONFLICT ("MigrationId") DO NOTHING;
        """, conn);
    cmd.Parameters.AddWithValue("id", migrationId);
    await cmd.ExecuteNonQueryAsync();
}

Console.WriteLine("Schema patches applied successfully.");

static async Task ExecuteAsync(NpgsqlConnection conn, string sql)
{
    await using var cmd = new NpgsqlCommand(sql, conn);
    await cmd.ExecuteNonQueryAsync();
}

static async Task<bool> ScalarBoolAsync(NpgsqlConnection conn, string sql)
{
    await using var cmd = new NpgsqlCommand(sql, conn);
    var result = await cmd.ExecuteScalarAsync();
    return result is bool b && b;
}

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Npgsql;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var configuration = new ConfigurationBuilder()
    .SetBasePath(repoRoot)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var baseConn = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is missing.");

var targetDatabase = GetArg("--database") ?? "erp_pro_perf";
var recreate = !args.Contains("--no-recreate", StringComparer.OrdinalIgnoreCase);
var connBuilder = new NpgsqlConnectionStringBuilder(baseConn);
var sourceDatabase = connBuilder.Database;
connBuilder.Database = targetDatabase;
var perfConn = connBuilder.ConnectionString;

var report = new PerfReport
{
    Timestamp = DateTimeOffset.UtcNow,
    SourceDatabase = sourceDatabase ?? "",
    TestDatabase = targetDatabase
};

Directory.CreateDirectory(Path.Combine(repoRoot, "docs"));

if (recreate)
{
    await RecreateDatabaseAsync(baseConn, targetDatabase);
}
await ApplyMigrationsAsync(perfConn);

await using var conn = new NpgsqlConnection(perfConn);
await conn.OpenAsync();

var beforeBytes = await ScalarLongAsync(conn, "SELECT pg_database_size(current_database());");
report.DatabaseBefore = FormatBytes(beforeBytes);

var seedTimer = Stopwatch.StartNew();
await SeedAsync(conn);
seedTimer.Stop();
report.SeedSeconds = Math.Round(seedTimer.Elapsed.TotalSeconds, 2);

var afterBytes = await ScalarLongAsync(conn, "SELECT pg_database_size(current_database());");
report.DatabaseAfter = FormatBytes(afterBytes);
report.RecordCounts = await CountRecordsAsync(conn);

await conn.ReloadTypesAsync();
await AnalyzeAsync(conn);

report.DatabaseMeasurements = await MeasureDatabaseAsync(conn);
report.DesktopMeasurements = await MeasureDesktopShapeAsync(conn);
report.WebMeasurements = await MeasureWebShapeAsync(conn);
report.Explain = await ExplainAsync(conn);
report.RootCauses = BuildRootCauses(report);
report.Questions = BuildAnswers(report);

var jsonPath = Path.Combine(repoRoot, "docs", "PERFORMANCE_SCALE_TEST_RESULTS.json");
var mdPath = Path.Combine(repoRoot, "docs", "PERFORMANCE_ROOT_CAUSE_REPORT.md");
var options = new JsonSerializerOptions { WriteIndented = true };
await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(report, options), Encoding.UTF8);
await File.WriteAllTextAsync(mdPath, ToMarkdown(report), Encoding.UTF8);

Console.WriteLine($"Performance scale test complete.");
Console.WriteLine($"JSON: {jsonPath}");
Console.WriteLine($"Report: {mdPath}");

string? GetArg(string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }
    return null;
}

static async Task RecreateDatabaseAsync(string baseConn, string dbName)
{
    var adminBuilder = new NpgsqlConnectionStringBuilder(baseConn) { Database = "postgres" };
    await using var admin = new NpgsqlConnection(adminBuilder.ConnectionString);
    await admin.OpenAsync();
    await ExecAsync(admin, """
        SELECT pg_terminate_backend(pid)
        FROM pg_stat_activity
        WHERE datname = @db AND pid <> pg_backend_pid();
        """, new NpgsqlParameter("db", dbName));
    await ExecAsync(admin, $"""DROP DATABASE IF EXISTS "{dbName.Replace("\"", "\"\"")}";""");
    await ExecAsync(admin, $"""CREATE DATABASE "{dbName.Replace("\"", "\"\"")}";""");
}

static async Task ApplyMigrationsAsync(string connectionString)
{
    var options = new DbContextOptionsBuilder<ErpDbContext>()
        .UseNpgsql(connectionString)
        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
        .Options;
    await using var context = new ErpDbContext(options);
    await context.Database.MigrateAsync();
}

static async Task SeedAsync(NpgsqlConnection conn)
{
    await ExecAsync(conn, "CREATE EXTENSION IF NOT EXISTS pgcrypto;");
    await ExecAsync(conn, """
        TRUNCATE
          sales.sales_invoice_roll_details,
          sales.sales_invoice_items,
          sales.sales_invoices,
          sales.warehouse_detailing_sessions,
          purchasing.purchase_order_lines,
          purchasing.purchase_orders,
          purchasing.purchase_invoice_items,
          purchasing.purchase_invoices,
          accounting.journal_entry_lines,
          accounting.journal_entries,
          inventory.stock_movement_lines,
          public."StockMovements",
          inventory.warehouse_stocks,
          inventory.inventory_alerts,
          inventory.inventory_audit_logs,
          inventory.inventory_timeline_events,
          inventory.fabric_batches,
          public."FabricRolls",
          china_import.container_items,
          china_import.container_fabric_type_lines,
          china_import.landing_costs,
          china_import.containers,
          audit.audit_logs,
          parties.customers,
          parties.china_suppliers,
          parties.suppliers,
          catalog.fabric_colors,
          catalog.fabric_items,
          catalog.fabric_categories,
          inventory.warehouses
        RESTART IDENTITY;
        """);

    await ExecAsync(conn, """
        INSERT INTO company.companies ("Id","Code","NameAr","NameEn","DefaultCurrency","CreatedAt","IsActive","IsArchived")
        SELECT '10000000-0000-0000-0000-000000000001','PERF','شركة اختبار الأداء','Performance Company','USD', now(), true, false
        WHERE NOT EXISTS (SELECT 1 FROM company.companies);

        INSERT INTO company.branches ("Id","CompanyId","Code","NameAr","NameEn","CreatedAt","IsActive","IsArchived")
        SELECT '10000000-0000-0000-0000-000000000002',
               (SELECT "Id" FROM company.companies ORDER BY "CreatedAt" LIMIT 1),
               'HQ','الفرع الرئيسي','HQ', now(), true, false
        WHERE NOT EXISTS (SELECT 1 FROM company.branches);

        INSERT INTO public."Accounts" ("Id","CompanyId","Code","NameAr","NameEn","AccountType","IsPostable","CreatedAt","IsActive","IsArchived")
        SELECT 'a1000001-0001-0001-0001-000000000001',
               (SELECT "Id" FROM company.companies ORDER BY "CreatedAt" LIMIT 1),
               '1300','مخزون','Inventory','Asset', true, now(), true, false
        WHERE NOT EXISTS (SELECT 1 FROM public."Accounts");
        """);

    await ExecAsync(conn, """
        CREATE TEMP TABLE perf_company AS SELECT "Id" AS id FROM company.companies ORDER BY "CreatedAt" LIMIT 1;
        CREATE TEMP TABLE perf_branch AS SELECT "Id" AS id FROM company.branches ORDER BY "CreatedAt" LIMIT 1;
        CREATE TEMP TABLE perf_account AS SELECT "Id" AS id FROM public."Accounts" ORDER BY "CreatedAt" LIMIT 1;
        """);

    await ExecAsync(conn, """
        CREATE TEMP TABLE perf_warehouses(rn int, id uuid);
        INSERT INTO perf_warehouses
        SELECT rn, gen_random_uuid() FROM generate_series(1,15) rn;

        INSERT INTO inventory.warehouses
        ("Id","BranchId","Code","NameAr","NameEn","City","Manager","IsDefault","CapacityRolls","CreatedAt","IsActive","IsArchived")
        SELECT id, (SELECT id FROM perf_branch), 'PERF-WH-' || lpad(rn::text,2,'0'),
               CASE WHEN rn = 1 THEN 'المستودع الرئيسي' ELSE 'مستودع أداء ' || rn END,
               'Performance Warehouse ' || rn,
               CASE WHEN rn <= 6 THEN 'Damascus' WHEN rn <= 11 THEN 'Regional' ELSE 'Transit' END,
               'Performance Manager ' || rn,
               rn = 1, 250000, now(), true, false
        FROM perf_warehouses;

        CREATE TEMP TABLE perf_category AS
        SELECT gen_random_uuid() AS id;

        INSERT INTO catalog.fabric_categories
        ("Id","CompanyId","Code","NameAr","NameEn","CreatedAt","IsActive","IsArchived")
        SELECT id, (SELECT id FROM perf_company), 'PERF-CAT', 'أقمشة اختبار الأداء', 'Performance Fabrics', now(), true, false
        FROM perf_category;

        CREATE TEMP TABLE perf_items(rn int, id uuid);
        INSERT INTO perf_items
        SELECT rn, gen_random_uuid() FROM generate_series(1,20) rn;

        INSERT INTO catalog.fabric_items
        ("Id","CompanyId","CategoryId","Code","NameAr","NameEn","DefaultUnit","CreatedAt","IsActive","IsArchived")
        SELECT id, (SELECT id FROM perf_company), (SELECT id FROM perf_category),
               'PERF-FAB-' || lpad(rn::text,2,'0'), 'نوع قماش ' || rn, 'Fabric Type ' || rn, 'meter', now(), true, false
        FROM perf_items;

        CREATE TEMP TABLE perf_colors(rn int, id uuid, item_id uuid);
        INSERT INTO perf_colors
        SELECT c.rn, gen_random_uuid(), i.id
        FROM generate_series(1,50) c(rn)
        JOIN perf_items i ON i.rn = ((c.rn - 1) % 20) + 1;

        INSERT INTO catalog.fabric_colors
        ("Id","FabricItemId","Code","NameAr","NameEn","CreatedAt","IsActive","IsArchived")
        SELECT id, item_id, 'CLR-' || lpad(rn::text,2,'0'), 'لون ' || rn, 'Color ' || rn, now(), true, false
        FROM perf_colors;
        """);

    await ExecAsync(conn, """
        CREATE TEMP TABLE perf_suppliers(rn int, id uuid);
        INSERT INTO perf_suppliers
        SELECT rn, gen_random_uuid() FROM generate_series(1,15) rn;

        INSERT INTO parties.suppliers
        ("Id","CompanyId","Code","Name","NameAr","NameEn","Status","Balance","BalanceCurrency","CreditLimit","CreditLimitCurrency",
         "PaymentTermsDays","CurrencyCode","Country","City","PayablesAccountId","OpeningBalancePosted","CreatedAt","IsActive","IsArchived")
        SELECT id, (SELECT id FROM perf_company), 'PERF-SUP-' || lpad(rn::text,2,'0'),
               'Performance Supplier ' || rn, 'مورد أداء ' || rn, 'Performance Supplier ' || rn,
               1, 0, 'USD', 100000, 'USD', 30, 'USD', 'China', 'Guangzhou',
               (SELECT id FROM perf_account), false, now(), true, false
        FROM perf_suppliers;

        INSERT INTO parties.china_suppliers ("Id","SupplierId","Port","DefaultIncoterm","LeadTimeDays","CreatedAt","IsActive","IsArchived")
        SELECT gen_random_uuid(), id, 'Ningbo', 'FOB', 45, now(), true, false
        FROM perf_suppliers;

        CREATE TEMP TABLE perf_customers(rn int, id uuid);
        INSERT INTO perf_customers
        SELECT rn, gen_random_uuid() FROM generate_series(1,500) rn;

        INSERT INTO parties.customers
        ("Id","CompanyId","Code","NameAr","NameEn","Type","Status","CreditLimit","CreditLimitCurrency","CreditLimitEnabled",
         "Balance","BalanceCurrency","PaymentTermsDays","Phone","AddressCity","OpeningBalancePosted","CreatedAt","IsActive","IsArchived")
        SELECT id, (SELECT id FROM perf_company), 'PERF-CUST-' || lpad(rn::text,4,'0'),
               'عميل أداء ' || rn, 'Performance Customer ' || rn, 1, 1,
               CASE WHEN rn <= 5 THEN 1000000 WHEN rn <= 55 THEN 250000 ELSE 25000 END,
               'USD', true, 0, 'USD', 30, '+963-900-' || lpad(rn::text,6,'0'), 'Damascus',
               false, now(), true, false
        FROM perf_customers;
        """);

    await ExecAsync(conn, """
        CREATE TEMP TABLE perf_containers(rn int, id uuid, supplier_id uuid);
        INSERT INTO perf_containers
        SELECT c.rn, gen_random_uuid(), s.id
        FROM generate_series(1,500) c(rn)
        JOIN perf_suppliers s ON s.rn = ((c.rn - 1) % 15) + 1;

        INSERT INTO china_import.containers
        ("Id","CompanyId","BranchId","SupplierId","ContainerNumber","Status","ShipmentDate","ExpectedArrival","ArrivalDate",
         "TotalRolls","TotalMeters","TotalWeightKg","Port","Notes","ExchangeRateToLocalCurrency","ChinaInvoiceAmountUsd",
         "ApprovedAt","CreatedAt","IsActive","IsArchived")
        SELECT id, (SELECT id FROM perf_company), (SELECT id FROM perf_branch), supplier_id,
               'PERF-CN-' || lpad(rn::text,4,'0'),
               CASE WHEN rn % 10 < 3 THEN 3 WHEN rn % 10 < 8 THEN 1 ELSE 0 END,
               now() - ((rn % 365) || ' days')::interval,
               now() + ((rn % 90) || ' days')::interval,
               CASE WHEN rn % 10 < 3 THEN now() - ((rn % 120) || ' days')::interval ELSE NULL END,
               400, 22000, 18000, 'Ningbo', 'Performance seed container', 1, 120000,
               CASE WHEN rn % 10 < 3 THEN now() - ((rn % 90) || ' days')::interval ELSE NULL END,
               now(), true, false
        FROM perf_containers;

        CREATE TEMP TABLE perf_batches(rn int, id uuid, container_id uuid, warehouse_id uuid);
        INSERT INTO perf_batches
        SELECT c.rn, gen_random_uuid(), c.id, w.id
        FROM perf_containers c
        JOIN perf_warehouses w ON w.rn = ((c.rn - 1) % 15) + 1;

        INSERT INTO inventory.fabric_batches
        ("Id","BatchNumber","SupplierId","ContainerId","ArrivalDate","LandingCostPerMeter","CurrencyCode",
         "TotalMeters","RollCount","WarehouseId","QualityStatus","Status","CreatedAt","IsActive","IsArchived")
        SELECT b.id, 'PERF-BATCH-' || lpad(b.rn::text,4,'0'), c.supplier_id, b.container_id,
               now() - ((b.rn % 365) || ' days')::interval, 2.5 + (b.rn % 8), 'USD',
               22000, 400, b.warehouse_id, 0, 0, now(), true, false
        FROM perf_batches b
        JOIN perf_containers c ON c.rn = b.rn;
        """);

    await ExecAsync(conn, """
        CREATE TEMP TABLE perf_roll_source AS
        SELECT n,
               c.id AS container_id,
               b.id AS batch_id,
               i.id AS item_id,
               col.id AS color_id,
               w.id AS warehouse_id,
               CASE WHEN n <= 60000 THEN 0 WHEN n <= 140000 THEN 2 WHEN n <= 180000 THEN 1 ELSE 3 END AS roll_status,
               CASE WHEN n > 180000 THEN 1 ELSE 0 END AS quality_status,
               35 + (n % 75)::numeric / 2 AS length_meters,
               1.5 + (n % 10)::numeric / 2 AS cost_per_meter
        FROM generate_series(1,200000) n
        JOIN perf_containers c ON c.rn = ((n - 1) / 400) + 1
        JOIN perf_batches b ON b.rn = c.rn
        JOIN perf_items i ON i.rn = ((n - 1) % 20) + 1
        JOIN perf_colors col ON col.rn = ((n - 1) % 50) + 1
        JOIN perf_warehouses w ON w.rn =
            CASE
              WHEN n <= 80000 THEN 1
              WHEN n <= 160000 THEN 2 + (((n - 80001) / 16000) % 5)
              WHEN n <= 180000 THEN 7 + (((n - 160001) / 4000) % 5)
              WHEN n <= 200000 THEN 12 + (((n - 180001) / 5000) % 4)
              ELSE 1
            END;

        CREATE TEMP TABLE perf_container_items(rn int, id uuid);
        INSERT INTO perf_container_items
        SELECT n, gen_random_uuid() FROM generate_series(1,200000) n;

        INSERT INTO china_import.container_items
        ("Id","ContainerId","LineNumber","FabricItemId","FabricColorId","RollCount","LengthMeters","WeightKg","LotCode","SupplierRollNumber","RowStatus","CreatedAt","IsActive","IsArchived")
        SELECT ci.id, r.container_id, ((r.n - 1) % 400) + 1, r.item_id, r.color_id,
               1, r.length_meters, r.length_meters * 0.25, 'LOT-' || lpad(((r.n - 1) / 400 + 1)::text,4,'0'),
               ((r.n - 1) % 400) + 1, 'Valid', now(), true, false
        FROM perf_roll_source r
        JOIN perf_container_items ci ON ci.rn = r.n;

        INSERT INTO public."FabricRolls"
        ("Id","ContainerId","ContainerItemId","FabricBatchId","FabricItemId","FabricColorId","WarehouseId",
         "RollNumber","Barcode","QrCode","LengthMeters","RemainingLengthMeters","CostPerMeter","SalePricePerMeter","WeightKg",
         "LotCode","Status","QualityStatus","ReservationStatus","CreatedAt","IsActive","IsArchived")
        SELECT gen_random_uuid(), r.container_id, ci.id, r.batch_id, r.item_id, r.color_id, r.warehouse_id,
               r.n, 'PERF-ROLL-' || lpad(r.n::text,6,'0'), 'PERF-QR-' || lpad(r.n::text,6,'0'),
               r.length_meters,
               CASE WHEN r.roll_status = 2 THEN r.length_meters * 0.45 ELSE r.length_meters END,
               r.cost_per_meter, r.cost_per_meter * 1.35, r.length_meters * 0.25,
               'LOT-' || lpad(((r.n - 1) / 400 + 1)::text,4,'0'),
               r.roll_status, r.quality_status, CASE WHEN r.roll_status = 1 THEN 1 ELSE 0 END,
               now() - ((r.n % 365) || ' days')::interval, true, false
        FROM perf_roll_source r
        JOIN perf_container_items ci ON ci.rn = r.n;
        """, timeoutSeconds: 0);

    await ExecAsync(conn, """
        INSERT INTO inventory.warehouse_stocks
        ("Id","WarehouseId","FabricItemId","FabricColorId","ContainerId","RollCount","TotalMeters","ReservedMeters","AvailableMeters","CreatedAt","IsActive","IsArchived")
        SELECT gen_random_uuid(), "WarehouseId", "FabricItemId", "FabricColorId", "ContainerId",
               count(*)::int,
               sum("LengthMeters"),
               sum(CASE WHEN "Status" = 1 THEN "RemainingLengthMeters" ELSE 0 END),
               sum(CASE WHEN "Status" IN (0,2) THEN "RemainingLengthMeters" ELSE 0 END),
               now(), true, false
        FROM public."FabricRolls"
        GROUP BY "WarehouseId", "FabricItemId", "FabricColorId", "ContainerId";
        """, timeoutSeconds: 0);

    await ExecAsync(conn, """
        CREATE TEMP TABLE perf_invoices(rn int, id uuid, customer_id uuid, warehouse_id uuid, container_id uuid);
        INSERT INTO perf_invoices
        SELECT n, gen_random_uuid(),
               CASE
                 WHEN n <= 4000 THEN (SELECT id FROM perf_customers WHERE rn = ((n - 1) % 5) + 1)
                 WHEN n <= 9000 THEN (SELECT id FROM perf_customers WHERE rn = 6 + ((n - 4001) % 50))
                 ELSE (SELECT id FROM perf_customers WHERE rn = 56 + ((n - 9001) % 445))
               END,
               (SELECT id FROM perf_warehouses WHERE rn = ((n - 1) % 15) + 1),
               (SELECT id FROM perf_containers WHERE rn = ((n - 1) % 500) + 1)
        FROM generate_series(1,10000) n;

        INSERT INTO sales.sales_invoices
        ("Id","CompanyId","BranchId","InvoiceNumber","CustomerId","WarehouseId","ChinaContainerId","InvoiceDate","PaymentType",
         "Status","SubTotal","DiscountTotal","TaxTotal","GrandTotal","DeliveredAt","DeliveredToName","DeliveryDriverName","DeliveryNotes",
         "CreatedAt","IsActive","IsArchived")
        SELECT id, (SELECT id FROM perf_company), (SELECT id FROM perf_branch),
               'PERF-SINV-' || lpad(rn::text,5,'0'), customer_id, warehouse_id, container_id,
               now() - ((rn % 180) || ' days')::interval, rn % 3,
               CASE WHEN rn <= 4000 THEN 0 WHEN rn <= 8000 THEN 2 ELSE 8 END,
               500 + (rn % 2000), 0, 0, 500 + (rn % 2000),
               CASE WHEN rn <= 3000 THEN now() - ((rn % 90) || ' days')::interval ELSE NULL END,
               CASE WHEN rn <= 3000 THEN 'مستلم ' || rn ELSE NULL END,
               CASE WHEN rn <= 3000 THEN 'سائق ' || (rn % 20) ELSE NULL END,
               CASE WHEN rn <= 3000 THEN 'Delivery performance seed' ELSE NULL END,
               now(), true, rn > 8000
        FROM perf_invoices;

        INSERT INTO sales.sales_invoice_items
        ("Id","SalesInvoiceId","LineNumber","FabricItemId","FabricColorId","RollCount","UnitPrice","OriginalUnitPrice","Unit","LineTotal","DiscountAmount","CreatedAt","IsActive","IsArchived")
        SELECT gen_random_uuid(), inv.id, line_no,
               (SELECT id FROM perf_items WHERE rn = (((inv.rn + line_no) - 1) % 20) + 1),
               (SELECT id FROM perf_colors WHERE rn = (((inv.rn + line_no) - 1) % 50) + 1),
               1 + ((inv.rn + line_no) % 4), 8 + (line_no % 5), 8 + (line_no % 5), 'meter',
               (1 + ((inv.rn + line_no) % 4)) * (8 + (line_no % 5)) * 40, 0, now(), true, false
        FROM perf_invoices inv
        CROSS JOIN generate_series(1,5) line_no;
        """, timeoutSeconds: 0);

    await ExecAsync(conn, """
        CREATE TEMP TABLE perf_purchase_orders(rn int, id uuid, supplier_id uuid);
        INSERT INTO perf_purchase_orders
        SELECT n, gen_random_uuid(), (SELECT id FROM perf_suppliers WHERE rn = ((n - 1) % 15) + 1)
        FROM generate_series(1,2000) n;

        INSERT INTO purchasing.purchase_orders
        ("Id","CompanyId","BranchId","OrderNumber","SupplierId","OrderDate","ExpectedDeliveryDate","TotalAmount","Status","Notes","CreatedAt","IsActive","IsArchived")
        SELECT id, (SELECT id FROM perf_company), (SELECT id FROM perf_branch), 'PERF-PO-' || lpad(rn::text,5,'0'),
               supplier_id, now() - ((rn % 90) || ' days')::interval, now() + ((rn % 45) || ' days')::interval,
               1000 + (rn % 20000), rn % 4, 'Performance purchase order', now(), true, false
        FROM perf_purchase_orders;

        INSERT INTO purchasing.purchase_order_lines
        ("Id","PurchaseOrderId","FabricItemId","Description","Quantity","UnitCost","LineTotal","CreatedAt","IsActive","IsArchived")
        SELECT gen_random_uuid(), po.id, (SELECT id FROM perf_items WHERE rn = (((po.rn + line_no) - 1) % 20) + 1),
               'Performance order line', 500 + line_no, 2 + line_no, (500 + line_no) * (2 + line_no), now(), true, false
        FROM perf_purchase_orders po
        CROSS JOIN generate_series(1,3) line_no;
        """, timeoutSeconds: 0);

    await ExecAsync(conn, """
        CREATE TEMP TABLE perf_movements(rn int, id uuid, warehouse_id uuid);
        INSERT INTO perf_movements
        SELECT n, gen_random_uuid(), (SELECT id FROM perf_warehouses WHERE rn = ((n - 1) % 15) + 1)
        FROM generate_series(1,50000) n;

        INSERT INTO public."StockMovements"
        ("Id","MovementNumber","MovementDate","Type","WarehouseId","Status","Reason","PostedAt","CreatedAt","IsActive","IsArchived")
        SELECT id, 'PERF-MOV-' || lpad(rn::text,6,'0'), now() - ((rn % 365) || ' days')::interval,
               rn % 6, warehouse_id, 6, 'Performance movement', now(), now(), true, false
        FROM perf_movements;

        INSERT INTO inventory.stock_movement_lines
        ("Id","MovementId","FabricItemId","FabricColorId","ContainerId","RollCount","QuantityMeters","UnitCost","TotalValue","CurrencyCode","CreatedAt","IsActive","IsArchived")
        SELECT gen_random_uuid(), m.id,
               (SELECT id FROM perf_items WHERE rn = ((m.rn - 1) % 20) + 1),
               (SELECT id FROM perf_colors WHERE rn = ((m.rn - 1) % 50) + 1),
               (SELECT id FROM perf_containers WHERE rn = ((m.rn - 1) % 500) + 1),
               1, 40 + (m.rn % 50), 3 + (m.rn % 8), (40 + (m.rn % 50)) * (3 + (m.rn % 8)), 'USD',
               now(), true, false
        FROM perf_movements m;

        INSERT INTO accounting.journal_entries
        ("Id","CompanyId","BranchId","EntryNumber","EntryDate","Description","Status","PostedAt","CreatedAt","IsActive","IsArchived")
        SELECT gen_random_uuid(), (SELECT id FROM perf_company), (SELECT id FROM perf_branch),
               'PERF-JE-' || lpad(n::text,6,'0'), now() - ((n % 365) || ' days')::interval,
               'Performance journal entry', CASE WHEN n % 5 = 0 THEN 0 ELSE 2 END, now(), now(), true, false
        FROM generate_series(1,15000) n;

        INSERT INTO audit.audit_logs
        ("Id","OccurredAt","Action","EntityType","EntityId","OldValuesJson","NewValuesJson","BranchId")
        SELECT gen_random_uuid(), now() - ((n % 365) || ' days')::interval,
               CASE WHEN n % 3 = 0 THEN 'Update' WHEN n % 3 = 1 THEN 'Create' ELSE 'View' END,
               CASE WHEN n % 4 = 0 THEN 'FabricRoll' WHEN n % 4 = 1 THEN 'SalesInvoice' WHEN n % 4 = 2 THEN 'Container' ELSE 'Customer' END,
               gen_random_uuid(), '{"old":true}', '{"new":true}', (SELECT id FROM perf_branch)
        FROM generate_series(1,100000) n;
        """, timeoutSeconds: 0);
}

static async Task AnalyzeAsync(NpgsqlConnection conn) => await ExecAsync(conn, "ANALYZE;", timeoutSeconds: 0);

static async Task<Dictionary<string, long>> CountRecordsAsync(NpgsqlConnection conn)
{
    var tables = new Dictionary<string, string>
    {
        ["fabric_rolls"] = "public.\"FabricRolls\"",
        ["containers"] = "china_import.containers",
        ["container_items"] = "china_import.container_items",
        ["sales_invoices"] = "sales.sales_invoices",
        ["sales_invoice_items"] = "sales.sales_invoice_items",
        ["customers"] = "parties.customers",
        ["purchase_orders"] = "purchasing.purchase_orders",
        ["stock_movements"] = "public.\"StockMovements\"",
        ["stock_movement_lines"] = "inventory.stock_movement_lines",
        ["journal_entries"] = "accounting.journal_entries",
        ["audit_logs"] = "audit.audit_logs"
    };

    var result = new Dictionary<string, long>();
    foreach (var (name, table) in tables)
        result[name] = await ScalarLongAsync(conn, $"SELECT count(*) FROM {table};");
    result["total"] = result.Values.Sum();
    return result;
}

static async Task<List<Measurement>> MeasureDatabaseAsync(NpgsqlConnection conn)
{
    var list = new List<Measurement>();
    var mainWarehouse = await ScalarGuidAsync(conn, "SELECT \"Id\" FROM inventory.warehouses ORDER BY \"Code\" LIMIT 1;");
    var stockRow = await ReadOneAsync(conn, """
        SELECT "WarehouseId","ContainerId","FabricItemId","FabricColorId"
        FROM inventory.warehouse_stocks
        ORDER BY "RollCount" DESC
        LIMIT 1;
        """);

    await MeasureQueryAsync(conn, list, "db_inventory_all_rolls_for_warehouse", """
        SELECT r.*, i."NameAr" AS fabric_name, c."NameAr" AS color_name, b."BatchNumber"
        FROM public."FabricRolls" r
        LEFT JOIN catalog.fabric_items i ON i."Id" = r."FabricItemId"
        LEFT JOIN catalog.fabric_colors c ON c."Id" = r."FabricColorId"
        LEFT JOIN inventory.fabric_batches b ON b."Id" = r."FabricBatchId"
        WHERE r."WarehouseId" = @warehouse
        ORDER BY r."RollNumber";
        """, new NpgsqlParameter("warehouse", mainWarehouse));

    await MeasureQueryAsync(conn, list, "db_inventory_filter_main_complete", """
        SELECT r."Id", r."RollNumber", r."Barcode", r."RemainingLengthMeters", r."CostPerMeter"
        FROM public."FabricRolls" r
        WHERE r."WarehouseId" = @warehouse AND r."Status" = 0 AND r."RemainingLengthMeters" > 0
        ORDER BY r."RollNumber";
        """, new NpgsqlParameter("warehouse", mainWarehouse));

    await MeasureQueryAsync(conn, list, "db_inventory_filter_main_complete_paged_50", """
        SELECT r."Id", r."RollNumber", r."Barcode", r."RemainingLengthMeters", r."CostPerMeter"
        FROM public."FabricRolls" r
        WHERE r."WarehouseId" = @warehouse AND r."Status" = 0 AND r."RemainingLengthMeters" > 0
        ORDER BY r."RollNumber"
        LIMIT 50;
        """, new NpgsqlParameter("warehouse", mainWarehouse));

    await MeasureQueryAsync(conn, list, "db_fabric_selection_by_stock", """
        SELECT r."Id", r."RollNumber", r."Barcode", r."RemainingLengthMeters"
        FROM public."FabricRolls" r
        WHERE r."WarehouseId" = @warehouse
          AND r."ContainerId" = @container
          AND r."FabricItemId" = @fabric
          AND r."FabricColorId" = @color
          AND r."RemainingLengthMeters" > 0
        ORDER BY r."RollNumber";
        """,
        new NpgsqlParameter("warehouse", (Guid)stockRow["WarehouseId"]),
        new NpgsqlParameter("container", (Guid)stockRow["ContainerId"]),
        new NpgsqlParameter("fabric", (Guid)stockRow["FabricItemId"]),
        new NpgsqlParameter("color", (Guid)stockRow["FabricColorId"]));

    await MeasureQueryAsync(conn, list, "db_sales_invoices_all_active", """
        SELECT inv.*, cust."NameAr" AS customer_name
        FROM sales.sales_invoices inv
        LEFT JOIN parties.customers cust ON cust."Id" = inv."CustomerId"
        WHERE inv."IsActive" = true AND inv."IsArchived" = false
        ORDER BY inv."InvoiceDate" DESC;
        """);

    await MeasureQueryAsync(conn, list, "db_month_sales_report_sql_aggregate", """
        SELECT date_trunc('day', inv."InvoiceDate") AS day,
               count(*) AS invoice_count,
               sum(inv."GrandTotal") AS total_amount,
               sum(items.item_count) AS line_count
        FROM sales.sales_invoices inv
        LEFT JOIN (
          SELECT "SalesInvoiceId", count(*) AS item_count
          FROM sales.sales_invoice_items
          GROUP BY "SalesInvoiceId"
        ) items ON items."SalesInvoiceId" = inv."Id"
        WHERE inv."InvoiceDate" >= now() - interval '30 days'
        GROUP BY day
        ORDER BY day;
        """);

    return list;
}

static async Task<List<Measurement>> MeasureDesktopShapeAsync(NpgsqlConnection conn)
{
    var list = new List<Measurement>();
    var mainWarehouse = await ScalarGuidAsync(conn, "SELECT \"Id\" FROM inventory.warehouses ORDER BY \"Code\" LIMIT 1;");
    var branch = await ScalarGuidAsync(conn, "SELECT \"Id\" FROM company.branches ORDER BY \"CreatedAt\" LIMIT 1;");

    await MeasureClientProjectionAsync(conn, list, "desktop_inventory_list_repository_shape", """
        SELECT r."Id", r."RollNumber", r."Barcode", r."FabricItemId", r."FabricColorId", r."FabricBatchId",
               r."LengthMeters", r."RemainingLengthMeters", r."CostPerMeter", r."Status"
        FROM public."FabricRolls" r
        WHERE r."WarehouseId" = @warehouse
        ORDER BY r."RollNumber";
        """, new NpgsqlParameter("warehouse", mainWarehouse));

    await MeasureClientProjectionAsync(conn, list, "desktop_warehouse_list_current_n_plus_one_shape", """
        SELECT w."Id",
               (SELECT count(*) FROM public."FabricRolls" r WHERE r."WarehouseId" = w."Id" AND r."RemainingLengthMeters" > 0) AS roll_count,
               (SELECT coalesce(sum(r."RemainingLengthMeters" * r."CostPerMeter"),0)::numeric(18,2) FROM public."FabricRolls" r WHERE r."WarehouseId" = w."Id" AND r."RemainingLengthMeters" > 0) AS inventory_value
        FROM inventory.warehouses w
        WHERE w."BranchId" = @branch
        ORDER BY w."Code";
        """, new NpgsqlParameter("branch", branch));

    await MeasureClientProjectionAsync(conn, list, "desktop_operations_center_stock_balance_shape", """
        SELECT s.*, i."NameAr" AS fabric_name, c."NameAr" AS color_name, w."NameAr" AS warehouse_name
        FROM inventory.warehouse_stocks s
        JOIN catalog.fabric_items i ON i."Id" = s."FabricItemId"
        JOIN catalog.fabric_colors c ON c."Id" = s."FabricColorId"
        JOIN inventory.warehouses w ON w."Id" = s."WarehouseId"
        WHERE s."WarehouseId" = @warehouse AND s."TotalMeters" > 0
        ORDER BY s."TotalMeters" DESC;
        """, new NpgsqlParameter("warehouse", mainWarehouse));

    await MeasureClientProjectionAsync(conn, list, "desktop_navigation_inventory_to_sales_back", """
        SELECT count(*) FROM public."FabricRolls" WHERE "WarehouseId" = @warehouse;
        SELECT count(*) FROM sales.sales_invoices WHERE "IsActive" = true AND "IsArchived" = false;
        SELECT count(*) FROM accounting.journal_entries WHERE "IsActive" = true AND "IsArchived" = false;
        SELECT count(*) FROM public."FabricRolls" WHERE "WarehouseId" = @warehouse;
        """, new NpgsqlParameter("warehouse", mainWarehouse));

    return list;
}

static async Task<List<WebMeasurement>> MeasureWebShapeAsync(NpgsqlConnection conn)
{
    var branch = await ScalarGuidAsync(conn, "SELECT \"Id\" FROM company.branches ORDER BY \"CreatedAt\" LIMIT 1;");
    var endpointQueries = new (string Name, string Sql)[]
    {
        ("GET /api/v1/inventory/warehouses", "SELECT json_agg(x) FROM (SELECT \"Id\",\"Code\",\"NameAr\",\"City\",\"IsDefault\" FROM inventory.warehouses WHERE \"BranchId\" = @branch ORDER BY \"Code\") x;"),
        ("GET /api/v1/inventory/dashboard", "SELECT json_build_object('totalRolls',(SELECT count(*) FROM public.\"FabricRolls\"),'totalMeters',(SELECT coalesce(sum(\"RemainingLengthMeters\"),0) FROM public.\"FabricRolls\"),'topFabrics',(SELECT json_agg(t) FROM (SELECT \"FabricItemId\", count(*) rolls, sum(\"RemainingLengthMeters\") meters FROM public.\"FabricRolls\" GROUP BY \"FabricItemId\" ORDER BY meters DESC LIMIT 10) t));"),
        ("GET /api/v1/inventory/alerts", "SELECT json_agg(x) FROM (SELECT \"Id\",\"Title\",\"Message\",\"CreatedAt\" FROM inventory.inventory_alerts WHERE \"BranchId\" = @branch AND \"IsAcknowledged\" = false ORDER BY \"CreatedAt\" DESC LIMIT 50) x;")
    };

    var result = new List<WebMeasurement>();
    foreach (var scenario in new[]
    {
        new NetworkScenario("Local WiFi", 10, 100),
        new NetworkScenario("Mobile 4G", 100, 10),
        new NetworkScenario("Mobile 3G", 200, 1)
    })
    {
        long totalBytes = 0;
        double totalBackend = 0;
        foreach (var (name, sql) in endpointQueries)
        {
            var sw = Stopwatch.StartNew();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("branch", branch);
            var json = (await cmd.ExecuteScalarAsync())?.ToString() ?? "null";
            sw.Stop();
            var bytes = Encoding.UTF8.GetByteCount(json);
            totalBytes += bytes;
            totalBackend += sw.Elapsed.TotalMilliseconds;
            result.Add(new WebMeasurement
            {
                Scenario = scenario.Name,
                Operation = name,
                BackendMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
                PayloadBytes = bytes,
                EstimatedNetworkMs = EstimateNetworkMs(bytes, scenario),
                EstimatedEndToEndMs = Math.Round(sw.Elapsed.TotalMilliseconds + EstimateNetworkMs(bytes, scenario), 2)
            });
        }

        result.Add(new WebMeasurement
        {
            Scenario = scenario.Name,
            Operation = "Inventory mobile page aggregate (3 parallel React Query calls)",
            BackendMs = Math.Round(totalBackend, 2),
            PayloadBytes = totalBytes,
            EstimatedNetworkMs = EstimateNetworkMs(totalBytes, scenario),
            EstimatedEndToEndMs = Math.Round(endpointQueries.Length * scenario.LatencyMs + totalBackend + EstimateTransferMs(totalBytes, scenario.BandwidthMbps), 2)
        });
    }
    return result;
}

static double EstimateNetworkMs(long bytes, NetworkScenario scenario) =>
    Math.Round(scenario.LatencyMs + EstimateTransferMs(bytes, scenario.BandwidthMbps), 2);

static double EstimateTransferMs(long bytes, double mbps) => bytes * 8d / (mbps * 1_000_000d) * 1000d;

static async Task<Dictionary<string, string>> ExplainAsync(NpgsqlConnection conn)
{
    var mainWarehouse = await ScalarGuidAsync(conn, "SELECT \"Id\" FROM inventory.warehouses ORDER BY \"Code\" LIMIT 1;");
    var result = new Dictionary<string, string>();
    result["inventory_filter"] = await ExplainAnalyzeAsync(conn, """
        SELECT r."Id", r."RollNumber", r."Barcode"
        FROM public."FabricRolls" r
        WHERE r."WarehouseId" = @warehouse AND r."Status" = 0 AND r."RemainingLengthMeters" > 0
        ORDER BY r."RollNumber"
        LIMIT 50;
        """, new NpgsqlParameter("warehouse", mainWarehouse));
    result["warehouse_stock"] = await ExplainAnalyzeAsync(conn, """
        SELECT * FROM inventory.warehouse_stocks
        WHERE "WarehouseId" = @warehouse AND "TotalMeters" > 0
        ORDER BY "TotalMeters" DESC;
        """, new NpgsqlParameter("warehouse", mainWarehouse));
    return result;
}

static List<RootCause> BuildRootCauses(PerfReport report) =>
[
    new()
    {
        Operation = "Inventory and warehouse lists",
        Category = "Database + Application + UI",
        Evidence = "Repository methods load all matching FabricRoll rows into memory, then aggregate/project in C#. Current 200K test confirms full-result operations dominate versus LIMIT 50.",
        Solution = "Server-side pagination and DTO projection, composite indexes on WarehouseId/Status/RemainingLengthMeters/RollNumber, and WPF DataGrid virtualization.",
        Impact = "Critical"
    },
    new()
    {
        Operation = "Operations center / stock balances",
        Category = "Application",
        Evidence = "GetFabricStockBalancesAsync loads warehouse_stocks and all rolls, then repeatedly filters rolls per stock row in memory.",
        Solution = "Move aggregation to SQL GROUP BY or materialized inventory summary, fetch only top/page rows for the current tab.",
        Impact = "Critical"
    },
    new()
    {
        Operation = "Web mobile inventory",
        Category = "Network + API",
        Evidence = "The mobile page issues warehouses/dashboard/alerts calls. Backend aggregation is the main cost now; on 3G payload and latency add visible delay.",
        Solution = "Keep calls parallel, add response compression, cache dashboard snapshots, and paginate any future roll list endpoint.",
        Impact = "High"
    }
];

static Dictionary<string, string> BuildAnswers(PerfReport report) => new()
{
    ["desktop_rows_displayed"] = "Current repository shape returns all rows for the selected warehouse; no server page contract exists for GetFabricRollsAsync.",
    ["wpf_virtualization"] = "Must be verified visually in XAML/runtime; code-level performance risk remains because ItemsSource can receive tens of thousands of rows.",
    ["filter_location"] = "Inventory filtering in tested repository methods is server-side only for simple WHERE clauses, but aggregation and many projections run client-side in C#.",
    ["ef_loading"] = "No lazy-loading proxy was found; slowness is from explicit ToListAsync full loads and follow-up dictionary/lookups.",
    ["sql_executed"] = "EXPLAIN ANALYZE output is included in the JSON report.",
    ["web_api_calls_inventory_page"] = "3 calls in web-client Inventory.tsx: warehouses, dashboard, alerts.",
    ["web_parallel_or_sequential"] = "React Query calls are declared independently and can run in parallel.",
    ["web_pagination"] = "Current inventory web page does not request fabric-roll pages; WPF/repository roll APIs are unpaged.",
    ["compression"] = "API Program.cs does not configure response compression.",
    ["json_response_size"] = "Measured in payloadBytes per web measurement in the JSON report.",
    ["fabric_roll_indexes"] = "Existing model defines indexes only on Barcode and FabricBatchId; no WarehouseId/Status composite index.",
    ["partial_index_complete"] = "No partial index for available/complete rolls exists in the current model.",
    ["columns_returned"] = "Several paths return full entities first, then map to DTOs.",
    ["includes"] = "No large Include chain was found in the inventory repository; related data is loaded with separate lookup queries.",
    ["n_plus_one"] = "Warehouse list uses per-warehouse stock/roll queries; operations center uses in-memory repeated filtering across loaded collections.",
    ["lookup_cache"] = "Catalog lookups are queried per operation; no cross-request cache is configured."
};

static async Task MeasureQueryAsync(NpgsqlConnection conn, List<Measurement> list, string name, string sql, params NpgsqlParameter[] parameters)
{
    var before = GC.GetTotalMemory(true);
    var sw = Stopwatch.StartNew();
    long rows = 0;
    long bytes = 0;
    await using var cmd = new NpgsqlCommand(sql, conn) { CommandTimeout = 0 };
    cmd.Parameters.AddRange(parameters);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        rows++;
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (!reader.IsDBNull(i))
                bytes += Encoding.UTF8.GetByteCount(Convert.ToString(reader.GetValue(i)) ?? "");
        }
    }
    sw.Stop();
    var after = GC.GetTotalMemory(false);
    list.Add(new Measurement
    {
        Operation = name,
        ElapsedMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
        Rows = rows,
        ApproxBytes = bytes,
        ManagedMemoryDeltaBytes = after - before
    });
}

static async Task MeasureClientProjectionAsync(NpgsqlConnection conn, List<Measurement> list, string name, string sql, params NpgsqlParameter[] parameters)
{
    var before = GC.GetTotalMemory(true);
    var sw = Stopwatch.StartNew();
    long rows = 0;
    var payload = new List<Dictionary<string, object?>>();
    await using var cmd = new NpgsqlCommand(sql, conn) { CommandTimeout = 0 };
    cmd.Parameters.AddRange(parameters);
    await using var reader = await cmd.ExecuteReaderAsync();
    do
    {
        while (await reader.ReadAsync())
        {
            rows++;
            var item = new Dictionary<string, object?>(StringComparer.Ordinal);
            for (var i = 0; i < reader.FieldCount; i++)
                item[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            payload.Add(item);
        }
    } while (await reader.NextResultAsync());
    var json = JsonSerializer.Serialize(payload);
    sw.Stop();
    var after = GC.GetTotalMemory(false);
    list.Add(new Measurement
    {
        Operation = name,
        ElapsedMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
        Rows = rows,
        ApproxBytes = Encoding.UTF8.GetByteCount(json),
        ManagedMemoryDeltaBytes = after - before
    });
}

static async Task<Dictionary<string, object>> ReadOneAsync(NpgsqlConnection conn, string sql)
{
    await using var cmd = new NpgsqlCommand(sql, conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync()) throw new InvalidOperationException("Expected one row.");
    var row = new Dictionary<string, object>();
    for (var i = 0; i < reader.FieldCount; i++)
        row[reader.GetName(i)] = reader.GetValue(i);
    return row;
}

static async Task<string> ExplainAnalyzeAsync(NpgsqlConnection conn, string sql, params NpgsqlParameter[] parameters)
{
    await using var cmd = new NpgsqlCommand("EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT) " + sql, conn) { CommandTimeout = 0 };
    cmd.Parameters.AddRange(parameters);
    var lines = new List<string>();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        lines.Add(reader.GetString(0));
    return string.Join(Environment.NewLine, lines);
}

static async Task ExecAsync(NpgsqlConnection conn, string sql, NpgsqlParameter? parameter = null, int timeoutSeconds = 300)
{
    await using var cmd = new NpgsqlCommand(sql, conn) { CommandTimeout = timeoutSeconds };
    if (parameter is not null)
        cmd.Parameters.Add(parameter);
    await cmd.ExecuteNonQueryAsync();
}

static async Task<long> ScalarLongAsync(NpgsqlConnection conn, string sql)
{
    await using var cmd = new NpgsqlCommand(sql, conn) { CommandTimeout = 0 };
    return Convert.ToInt64(await cmd.ExecuteScalarAsync());
}

static async Task<Guid> ScalarGuidAsync(NpgsqlConnection conn, string sql)
{
    await using var cmd = new NpgsqlCommand(sql, conn);
    return (Guid)(await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Guid scalar returned null."));
}

static string FormatBytes(long bytes) => bytes >= 1024L * 1024 * 1024
    ? $"{bytes / 1024d / 1024d / 1024d:F2} GB"
    : $"{bytes / 1024d / 1024d:F2} MB";

static string ToMarkdown(PerfReport report)
{
    var sb = new StringBuilder();
    sb.AppendLine("# Performance Root Cause Report");
    sb.AppendLine();
    sb.AppendLine($"- Timestamp UTC: `{report.Timestamp:O}`");
    sb.AppendLine($"- Test database: `{report.TestDatabase}`");
    sb.AppendLine($"- Seed time: `{report.SeedSeconds:N2}s`");
    sb.AppendLine($"- Database size: `{report.DatabaseBefore}` -> `{report.DatabaseAfter}`");
    sb.AppendLine();
    sb.AppendLine("## Record Counts");
    foreach (var item in report.RecordCounts)
        sb.AppendLine($"- `{item.Key}`: `{item.Value:N0}`");
    sb.AppendLine();
    sb.AppendLine("## Measured Bottlenecks");
    foreach (var m in report.DatabaseMeasurements.Concat(report.DesktopMeasurements).OrderByDescending(x => x.ElapsedMs))
        sb.AppendLine($"- `{m.Operation}`: `{m.ElapsedMs:N2} ms`, rows `{m.Rows:N0}`, payload `{FormatBytes(m.ApproxBytes)}`, managed memory delta `{FormatBytes(Math.Abs(m.ManagedMemoryDeltaBytes))}`");
    sb.AppendLine();
    sb.AppendLine("## Web Measurements");
    foreach (var m in report.WebMeasurements)
        sb.AppendLine($"- `{m.Scenario}` `{m.Operation}`: backend `{m.BackendMs:N2} ms`, payload `{FormatBytes(m.PayloadBytes)}`, estimated E2E `{m.EstimatedEndToEndMs:N2} ms`");
    sb.AppendLine();
    sb.AppendLine("## Root Causes");
    foreach (var r in report.RootCauses)
        sb.AppendLine($"- **{r.Impact} / {r.Category} / {r.Operation}**: {r.Evidence} Solution: {r.Solution}");
    sb.AppendLine();
    sb.AppendLine("## Answers");
    foreach (var q in report.Questions)
        sb.AppendLine($"- `{q.Key}`: {q.Value}");
    sb.AppendLine();
    sb.AppendLine("## EXPLAIN Evidence");
    foreach (var e in report.Explain)
    {
        sb.AppendLine($"### {e.Key}");
        sb.AppendLine("```text");
        sb.AppendLine(e.Value);
        sb.AppendLine("```");
    }
    return sb.ToString();
}

sealed record NetworkScenario(string Name, int LatencyMs, double BandwidthMbps);

sealed class PerfReport
{
    public DateTimeOffset Timestamp { get; set; }
    public string SourceDatabase { get; set; } = "";
    public string TestDatabase { get; set; } = "";
    public string DatabaseBefore { get; set; } = "";
    public string DatabaseAfter { get; set; } = "";
    public double SeedSeconds { get; set; }
    public Dictionary<string, long> RecordCounts { get; set; } = [];
    public List<Measurement> DatabaseMeasurements { get; set; } = [];
    public List<Measurement> DesktopMeasurements { get; set; } = [];
    public List<WebMeasurement> WebMeasurements { get; set; } = [];
    public Dictionary<string, string> Explain { get; set; } = [];
    public List<RootCause> RootCauses { get; set; } = [];
    public Dictionary<string, string> Questions { get; set; } = [];
}

sealed class Measurement
{
    public string Operation { get; set; } = "";
    public double ElapsedMs { get; set; }
    public long Rows { get; set; }
    public long ApproxBytes { get; set; }
    public long ManagedMemoryDeltaBytes { get; set; }
}

sealed class WebMeasurement
{
    public string Scenario { get; set; } = "";
    public string Operation { get; set; } = "";
    public double BackendMs { get; set; }
    public long PayloadBytes { get; set; }
    public double EstimatedNetworkMs { get; set; }
    public double EstimatedEndToEndMs { get; set; }
}

sealed class RootCause
{
    public string Operation { get; set; } = "";
    public string Category { get; set; } = "";
    public string Evidence { get; set; } = "";
    public string Solution { get; set; } = "";
    public string Impact { get; set; } = "";
}

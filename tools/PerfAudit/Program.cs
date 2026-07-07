using System.Text;
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

var sb = new StringBuilder();
void W(string s = "") => sb.AppendLine(s);

var builder = new NpgsqlConnectionStringBuilder(baseConn);
var configuredDb = builder.Database;

// 1) Discover the real database (configured one may not exist).
string? targetDb = null;
try
{
    await using var probe = new NpgsqlConnection(baseConn);
    await probe.OpenAsync();
    targetDb = configuredDb;
}
catch (PostgresException ex) when (ex.SqlState == "3D000")
{
    W($"[warn] configured database '{configuredDb}' does not exist. Scanning server...");
}

if (targetDb is null)
{
    var adminBuilder = new NpgsqlConnectionStringBuilder(baseConn) { Database = "postgres" };
    await using var admin = new NpgsqlConnection(adminBuilder.ConnectionString);
    await admin.OpenAsync();
    var dbs = new List<string>();
    await using (var cmd = new NpgsqlCommand(
        "SELECT datname FROM pg_database WHERE datistemplate = false AND datname <> 'postgres' ORDER BY datname;", admin))
    await using (var r = await cmd.ExecuteReaderAsync())
        while (await r.ReadAsync()) dbs.Add(r.GetString(0));

    W($"[info] databases on server: {string.Join(", ", dbs)}");
    foreach (var db in dbs)
    {
        var b = new NpgsqlConnectionStringBuilder(baseConn) { Database = db };
        try
        {
            await using var c = new NpgsqlConnection(b.ConnectionString);
            await c.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name = 'china_import';", c);
            var has = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            if (has > 0) { targetDb = db; break; }
        }
        catch { /* ignore */ }
    }
}

if (targetDb is null)
{
    W("[fatal] could not locate an ERP database (no db with schema 'china_import').");
    await File.WriteAllTextAsync(Path.Combine(repoRoot, "_perf_audit.txt"), sb.ToString(), Encoding.UTF8);
    Console.WriteLine("No ERP database found. See _perf_audit.txt");
    return;
}

var connBuilder = new NpgsqlConnectionStringBuilder(baseConn) { Database = targetDb };
await using var conn = new NpgsqlConnection(connBuilder.ConnectionString);
await conn.OpenAsync();
W($"=== ERP PERFORMANCE AUDIT — database '{targetDb}' — {DateTime.Now:yyyy-MM-dd HH:mm} ===");
W();

// Server version + key settings
W("## SERVER / CONFIG");
await ScalarLine(conn, "version", "SELECT version();");
foreach (var setting in new[] { "max_connections", "shared_buffers", "work_mem", "effective_cache_size", "maintenance_work_mem", "random_page_cost", "track_activity_query_size" })
    await ShowSetting(conn, setting);
W();

// 2) Tables: size + estimated rows + scan stats
W("## TABLES (by total size)  [schema.table | est_rows | seq_scan | idx_scan | total | table | indexes]");
await using (var cmd = new NpgsqlCommand("""
    SELECT n.nspname AS schema, c.relname AS tbl,
           COALESCE(s.n_live_tup, 0) AS est_rows,
           COALESCE(s.seq_scan, 0) AS seq_scan,
           COALESCE(s.idx_scan, 0) AS idx_scan,
           pg_total_relation_size(c.oid) AS total_bytes,
           pg_relation_size(c.oid) AS table_bytes,
           pg_indexes_size(c.oid) AS index_bytes
    FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    LEFT JOIN pg_stat_user_tables s ON s.relid = c.oid
    WHERE c.relkind = 'r' AND n.nspname NOT IN ('pg_catalog','information_schema')
    ORDER BY pg_total_relation_size(c.oid) DESC;
    """, conn))
await using (var r = await cmd.ExecuteReaderAsync())
{
    while (await r.ReadAsync())
    {
        W($"{r.GetString(0)}.{r.GetString(1),-34} | rows={r.GetInt64(2),8} | seq={r.GetInt64(3),7} | idx={r.GetInt64(4),8} | " +
          $"total={Kb(r.GetInt64(5)),9} | table={Kb(r.GetInt64(6)),9} | idx={Kb(r.GetInt64(7)),9}");
    }
}
W();

// 3) Indexes
W("## INDEXES  [schema.table | index | definition | idx_scan]");
await using (var cmd = new NpgsqlCommand("""
    SELECT i.schemaname, i.tablename, i.indexname, i.indexdef, COALESCE(s.idx_scan,0) AS scans
    FROM pg_indexes i
    LEFT JOIN pg_stat_user_indexes s
      ON s.schemaname = i.schemaname AND s.indexrelname = i.indexname
    WHERE i.schemaname NOT IN ('pg_catalog','information_schema')
    ORDER BY i.schemaname, i.tablename, i.indexname;
    """, conn))
await using (var r = await cmd.ExecuteReaderAsync())
{
    while (await r.ReadAsync())
        W($"{r.GetString(0)}.{r.GetString(1)} | {r.GetString(2)} | {r.GetString(3)} | scans={r.GetInt64(4)}");
}
W();

// 4) Foreign keys
W("## FOREIGN KEYS  [table | constraint | definition]");
await using (var cmd = new NpgsqlCommand("""
    SELECT conrelid::regclass::text AS tbl, conname, pg_get_constraintdef(oid) AS def
    FROM pg_constraint WHERE contype = 'f'
    ORDER BY conrelid::regclass::text, conname;
    """, conn))
await using (var r = await cmd.ExecuteReaderAsync())
{
    while (await r.ReadAsync())
        W($"{r.GetString(0)} | {r.GetString(1)} | {r.GetString(2)}");
}
W();

// 5) Tables with high seq_scan and meaningful rows (missing-index candidates)
W("## HIGH SEQUENTIAL-SCAN TABLES (seq_scan-heavy, rows>50)");
await using (var cmd = new NpgsqlCommand("""
    SELECT schemaname, relname, seq_scan, idx_scan, n_live_tup
    FROM pg_stat_user_tables
    WHERE n_live_tup > 50 AND seq_scan > COALESCE(idx_scan,0)
    ORDER BY seq_scan DESC LIMIT 40;
    """, conn))
await using (var r = await cmd.ExecuteReaderAsync())
{
    while (await r.ReadAsync())
        W($"{r.GetString(0)}.{r.GetString(1),-34} | seq={r.GetInt64(2),8} | idx={r.GetInt64(3),8} | rows={r.GetInt64(4),8}");
}
W();

await File.WriteAllTextAsync(Path.Combine(repoRoot, "_perf_audit.txt"), sb.ToString(), Encoding.UTF8);
Console.WriteLine($"Audit written to _perf_audit.txt (database '{targetDb}').");

static string Kb(long bytes) => bytes >= 1024L * 1024
    ? $"{bytes / 1024.0 / 1024.0:F1}MB"
    : $"{bytes / 1024.0:F0}KB";

async Task ScalarLine(NpgsqlConnection c, string label, string sql)
{
    await using var cmd = new NpgsqlCommand(sql, c);
    var v = await cmd.ExecuteScalarAsync();
    W($"{label}: {v}");
}

async Task ShowSetting(NpgsqlConnection c, string name)
{
    await using var cmd = new NpgsqlCommand($"SHOW {name};", c);
    var v = await cmd.ExecuteScalarAsync();
    W($"{name}: {v}");
}

using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.DependencyInjection;
using ERPSystem.Infrastructure.DependencyInjection;
using ERPSystem.Infrastructure.E2E;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var mode = args.FirstOrDefault(a => a.StartsWith("--")) ?? "--help";

var configuration = new ConfigurationBuilder()
    .SetBasePath(repoRoot)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Local.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

using var tunnel = ERPSystem.Services.SshTunnelService.StartIfConfigured(configuration);

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddSingleton<ICurrentUserService, E2ETestCurrentUserService>();
services.AddSingleton<ICurrentBranchService, E2ETestBranchService>();
services.AddSingleton<IPermissionService, E2EAllowAllPermissionService>();
services.AddInfrastructure(configuration);
services.AddApplication();
services.AddScoped<Phase2TaxE2ECertificationRunner>();

await using var provider = services.BuildServiceProvider();
await WaitForDatabaseAsync(provider, configuration);

using var scope = provider.CreateScope();
var sp = scope.ServiceProvider;
var context = sp.GetRequiredService<ErpDbContext>();

return mode switch
{
    "--seed" => await SeedAsync(context),
    "--run" => await RunAsync(sp, repoRoot),
    "--verify" => await VerifyAsync(sp, repoRoot),
    "--cleanup" => await CleanupAsync(context),
    _ => PrintHelp()
};

static int PrintHelp()
{
    Console.WriteLine("Phase 2 Tax E2E Certification");
    Console.WriteLine("  --seed    Idempotent test company seed");
    Console.WriteLine("  --run     Execute all E2E scenarios");
    Console.WriteLine("  --verify  Read-only verification of last run artifacts");
    Console.WriteLine("  --cleanup Cancel draft test invoices only");
    return 0;
}

async Task<int> SeedAsync(ErpDbContext context)
{
    Phase2E2ETestCompanySeeder.GuardNotProduction();
    var result = await Phase2E2ETestCompanySeeder.SeedAsync(context);
    Console.WriteLine($"Seeded {Phase2E2ETestCompanyIds.CompanyNameEn}");
    Console.WriteLine($"  CompanyId:   {result.CompanyId}");
    Console.WriteLine($"  CustomerId:  {result.CustomerId}");
    Console.WriteLine($"  ContainerId: {result.ContainerId}");
    Console.WriteLine($"  Rolls:       {result.AvailableRolls}");
    return 0;
}

async Task<int> RunAsync(IServiceProvider sp, string repoRoot)
{
    var context = sp.GetRequiredService<ErpDbContext>();
    await Phase2E2ETestCompanySeeder.SeedAsync(context);

    var runner = sp.GetRequiredService<Phase2TaxE2ECertificationRunner>();
    var result = await runner.RunAllScenariosAsync();

    Console.WriteLine($"RunId: {result.RunId}");
    foreach (var s in result.Scenarios)
        Console.WriteLine($"  [{ (s.Passed ? "PASS" : "FAIL") }] {s.Name}: {s.Details}");

    Console.WriteLine($"  [{(result.Concurrency.Passed ? "PASS" : "FAIL")}] Concurrency: journals={result.Concurrency.JournalEntryCount} snapshots={result.Concurrency.TaxSnapshotCount}");

    if (result.ScenarioA.InvoiceId != Guid.Empty)
    {
        var proof = await runner.BuildCrossLayerProofAsync(result.ScenarioA.InvoiceId);
        await WriteCrossLayerProofAsync(repoRoot, proof);
        Console.WriteLine($"  Cross-layer proof: {(proof.AllMatch ? "PASS" : "FAIL")} invoice={proof.InvoiceNumber}");
    }

    var artifactsDir = Path.Combine(repoRoot, "artifacts");
    Directory.CreateDirectory(artifactsDir);
    await File.WriteAllTextAsync(Path.Combine(artifactsDir, "phase2-e2e-last-run-id.txt"), result.RunId);

    return result.AllPassed ? 0 : 1;
}

async Task<int> VerifyAsync(IServiceProvider sp, string repoRoot)
{
    var runner = sp.GetRequiredService<Phase2TaxE2ECertificationRunner>();
    var artifactsDir = Path.Combine(repoRoot, "artifacts");
    var runIdFile = Path.Combine(artifactsDir, "phase2-e2e-last-run-id.txt");
    if (!File.Exists(runIdFile))
    {
        Console.WriteLine("No prior run found.");
        return 1;
    }

    var runId = await File.ReadAllTextAsync(runIdFile);
    var context = sp.GetRequiredService<ErpDbContext>();
    var invoice = await context.Set<ERPSystem.Infrastructure.Persistence.Models.Sales.SalesInvoiceEntity>()
        .AsNoTracking()
        .Where(i => i.CompanyId == Phase2E2ETestCompanyIds.CompanyId
                    && i.InvoiceNumber == $"E2E-TAX-{runId.Trim()}-A")
        .FirstOrDefaultAsync();

    if (invoice is null)
    {
        Console.WriteLine($"Scenario A invoice not found for run {runId.Trim()}");
        return 1;
    }

    var proof = await runner.BuildCrossLayerProofAsync(invoice.Id);
    await WriteCrossLayerProofAsync(repoRoot, proof);
    Console.WriteLine($"Verify: {(proof.AllMatch ? "PASS" : "FAIL")}");
    return proof.AllMatch ? 0 : 1;
}

async Task WaitForDatabaseAsync(ServiceProvider provider, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is not configured.");

    for (var attempt = 1; attempt <= 30; attempt++)
    {
        try
        {
            await using var conn = new Npgsql.NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            return;
        }
        catch when (attempt < 30)
        {
            Console.WriteLine($"Waiting for database (attempt {attempt}/30)...");
            await Task.Delay(2000);
        }
    }

    throw new InvalidOperationException("Database is not reachable on configured connection string.");
}

async Task<int> CleanupAsync(ErpDbContext context)
{
    Phase2E2ETestCompanySeeder.GuardNotProduction(Phase2E2ETestCompanyIds.CompanyId);
    var drafts = await context.Set<ERPSystem.Infrastructure.Persistence.Models.Sales.SalesInvoiceEntity>()
        .Where(i => i.CompanyId == Phase2E2ETestCompanyIds.CompanyId
                    && i.Status == (int)ERPSystem.Domain.Enums.SalesInvoiceStatus.Draft)
        .ToListAsync();

    foreach (var d in drafts)
        context.Remove(d);

    await context.SaveChangesAsync();
    Console.WriteLine($"Removed {drafts.Count} draft test invoices (posted journals preserved).");
    return 0;
}

async Task WriteCrossLayerProofAsync(string repoRoot, CrossLayerProof proof)
{
    var path = Path.Combine(repoRoot, "artifacts", "phase2-e2e-cross-layer-proof.md");
    var md = $"""
        # Phase 2 E2E Cross-Layer Proof

        **Invoice:** {proof.InvoiceNumber} (`{proof.InvoiceId}`)

        | Field | DB | Journal | PDF | Report | Match |
        |-------|---:|--------:|----:|-------:|-------|
        | Grand Total | {proof.DbGrandTotal:N2} | AR Dr {proof.JournalArDebit:N2} | {proof.PdfGrandTotal:N2} | — | {(Math.Abs(proof.DbGrandTotal - proof.PdfGrandTotal) < 0.01m ? "PASS" : "FAIL")} |
        | Tax Total | {proof.DbTaxTotal:N2} | — | — | {proof.ReportTaxAmount:N2} | {(Math.Abs(proof.DbTaxTotal - proof.ReportTaxAmount) < 0.01m ? "PASS" : "FAIL")} |

        **Overall:** {(proof.AllMatch ? "PASS" : "FAIL")}
        """;
    await File.WriteAllTextAsync(path, md);
}

sealed class E2ETestCurrentUserService : ICurrentUserService
{
    public Guid? UserId => DatabaseSeeder.AdminUserId;
    public string? Username => "admin";
    public bool IsAuthenticated => true;
}

sealed class E2ETestBranchService : ICurrentBranchService
{
    public Guid? CompanyId => Phase2E2ETestCompanyIds.CompanyId;
    public Guid? BranchId => Phase2E2ETestCompanyIds.BranchId;
}

sealed class E2EAllowAllPermissionService : IPermissionService
{
    public Task<bool> CanAsync(string permissionCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
    public Task EnsureCanAsync(string permissionCode, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

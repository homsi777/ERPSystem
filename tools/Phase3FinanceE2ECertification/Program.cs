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

var baseConnection = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
var testConnection = E2EProductionGuard.ResolvePhase3TestConnection(baseConnection);

using var tunnel = ERPSystem.Services.SshTunnelService.StartIfConfigured(configuration);

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddSingleton<ICurrentUserService, E2ETestCurrentUserService>();
services.AddSingleton<ICurrentBranchService, E2ETestBranchService>();
services.AddSingleton<Phase3E2EPermissionGate>();
services.AddSingleton<IPermissionService>(sp => sp.GetRequiredService<Phase3E2EPermissionGate>());
services.AddInfrastructure(new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = testConnection })
    .Build());
services.AddApplication();
services.AddScoped<Phase3FinanceE2ECertificationRunner>();

await using var provider = services.BuildServiceProvider();
await WaitForDatabaseAsync(provider, testConnection);

using var scope = provider.CreateScope();
var sp = scope.ServiceProvider;
var context = sp.GetRequiredService<ErpDbContext>();

return mode switch
{
    "--seed" => await SeedAsync(context),
    "--run" => await RunAsync(sp),
    "--guard-check" => GuardCheck(testConnection),
    _ => PrintHelp()
};

static int PrintHelp()
{
    Console.WriteLine("Phase 3 Finance E2E Certification");
    Console.WriteLine("  --seed         Idempotent test company seed on erp_pro_phase3_e2e");
    Console.WriteLine("  --run          Execute 28-test matrix");
    Console.WriteLine("  --guard-check  Verify production guard blocks erp_pro");
    return 0;
}

static int GuardCheck(string testConnection)
{
    try
    {
        E2EProductionGuard.GuardWritableE2E("Host=localhost;Database=erp_pro;Username=x;Password=x");
        Console.WriteLine("FAIL: guard did not block erp_pro");
        return 1;
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"PASS: {ex.Message}");
    }

    E2EProductionGuard.GuardWritableE2E(testConnection);
    Console.WriteLine($"PASS: test connection allowed ({E2EProductionGuard.Phase3TestDatabaseName})");
    return 0;
}

async Task<int> SeedAsync(ErpDbContext context)
{
    var result = await Phase3FinanceE2ETestCompanySeeder.SeedAsync(context);
    Console.WriteLine($"Seeded {Phase3FinanceE2ETestCompanyIds.CompanyNameEn}");
    Console.WriteLine($"  CompanyId:  {result.CompanyId}");
    Console.WriteLine($"  Cashbox A:  {result.CashboxAId}");
    Console.WriteLine($"  Cashbox B:  {result.CashboxBId}");
    Console.WriteLine($"  Bank:       {result.BankAccountId}");
    return 0;
}

async Task<int> RunAsync(IServiceProvider sp)
{
    var context = sp.GetRequiredService<ErpDbContext>();
    await Phase3FinanceE2ETestCompanySeeder.SeedAsync(context);

    var runner = sp.GetRequiredService<Phase3FinanceE2ECertificationRunner>();
    var result = await runner.RunAllMatrixAsync();

    Console.WriteLine($"RunId: {result.RunId}");
    Console.WriteLine($"Passed: {result.PassedCount} Failed: {result.FailedCount}");
    foreach (var m in result.Matrix.Where(x => !x.Passed))
        Console.WriteLine($"  FAIL #{m.Index} {m.Name}: {m.Details}");

    return result.AllPassed ? 0 : 1;
}

static async Task WaitForDatabaseAsync(ServiceProvider provider, string connectionString)
{
    for (var i = 0; i < 30; i++)
    {
        try
        {
            using var scope = provider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
            if (await ctx.Database.CanConnectAsync()) return;
        }
        catch { /* retry */ }
        await Task.Delay(1000);
    }
    throw new InvalidOperationException($"Cannot connect to {connectionString}");
}

sealed class E2ETestCurrentUserService : ICurrentUserService
{
    public Guid? UserId => DatabaseSeeder.AdminUserId;
    public string? Username => "admin";
    public bool IsAuthenticated => true;
}

sealed class E2ETestBranchService : ICurrentBranchService
{
    public Guid? CompanyId => Phase3FinanceE2ETestCompanyIds.CompanyId;
    public Guid? BranchId => Phase3FinanceE2ETestCompanyIds.BranchId;
}

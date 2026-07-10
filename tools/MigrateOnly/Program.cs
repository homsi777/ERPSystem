using ERPSystem.Infrastructure.DependencyInjection;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

var configuration = new ConfigurationBuilder()
    .SetBasePath(repoRoot)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Local.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

using var tunnel = SshTunnelService.StartIfConfigured(configuration);

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddInfrastructure(configuration);

await using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ErpDbContext>();

var pending = await context.Database.GetPendingMigrationsAsync();
Console.WriteLine("Pending migrations: " + pending.Count());
foreach (var id in pending)
    Console.WriteLine("  " + id);

if (pending.Any())
{
    Console.WriteLine("Applying...");
    await context.Database.MigrateAsync();
}

var hasColumn = await context.Database.SqlQueryRaw<bool>(
    """
    SELECT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'sales'
          AND table_name = 'sales_invoice_items'
          AND column_name = 'ChinaContainerId'
    ) AS "Value"
    """).SingleAsync();

Console.WriteLine("sales_invoice_items.ChinaContainerId exists: " + hasColumn);

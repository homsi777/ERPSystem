using ERPSystem.Infrastructure.DependencyInjection;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

var configuration = new ConfigurationBuilder()
    .SetBasePath(repoRoot)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddInfrastructure(configuration);

await using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

await context.Database.MigrateAsync();
await ImportCatalogDevelopmentCleanup.RunAsync(context, logger);

Console.WriteLine();
Console.WriteLine("Done. Fabric catalog, containers, and related inventory/sales data were removed.");
Console.WriteLine("Company, users, warehouses, accounting, customers, and suppliers were kept.");

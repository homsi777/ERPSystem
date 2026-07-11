using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.DependencyInjection;
using ERPSystem.Application.Diagnostics;
using ERPSystem.Application.Results;
using ERPSystem.Diagnostics.Performance;
using ERPSystem.Infrastructure.DependencyInjection;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using ERPSystem.Services.Purchases;
using ERPSystem.Services.Sales;
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

using var tunnel = await SshTunnelService.StartIfConfiguredAsync(configuration);

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddSingleton<IWpfPerformanceProfiler, WpfPerformanceProfiler>();
services.AddSingleton<ICurrentUserService, WpfCurrentUserService>();
services.AddSingleton<ICurrentBranchService, WpfCurrentBranchService>();
services.AddInfrastructure(configuration);
services.AddApplication();
services.AddSingleton<SalesUiService>();
services.AddSingleton<CustomerUiService>();
services.AddSingleton<PurchaseUiService>();

await using var provider = services.BuildServiceProvider();
AppServices.Initialize(provider);

EfQueryTelemetry.EnsureStarted();

var profiler = provider.GetRequiredService<IWpfPerformanceProfiler>();
var sales = provider.GetRequiredService<SalesUiService>();

Console.WriteLine("WPF Perf Capture — startup + OperationsCenter (cloud)");

using (var migrateScope = ScreenLoadProfiler.Begin("App.Startup.MigrateAndSeed"))
{
    using (migrateScope?.MeasureDataLoad())
        await provider.MigrateAndSeedAsync();
}

using (var currencyScope = ScreenLoadProfiler.Begin("App.Startup.CurrencyCatalog"))
{
    using (currencyScope?.MeasureDataLoad())
        await ERPSystem.Services.Settings.CurrencyCatalog.RefreshAsync();
}

using (var referenceScope = ScreenLoadProfiler.Begin("App.Startup.ReferenceDataCatalog"))
{
    using (referenceScope?.MeasureDataLoad())
        await ERPSystem.Services.Settings.ReferenceDataCatalog.RefreshAsync();
}

var listResult = await sales.GetListAsync(null, null, 1, 100);
if (listResult.IsSuccess && listResult.Value?.Items.Count > 0)
{
    var invoiceId = listResult.Value.Items[0].Id;
    using var scope = ScreenLoadProfiler.Begin("Sales.OperationsCenter");
    var ocResult = await ScreenLoadProfiler.MeasureLoadAsync(scope, () => sales.GetOperationsCenterAsync(invoiceId));
    scope?.IncrementServiceCalls();
    scope?.SetRowsReturned(ocResult.IsSuccess ? 1 : 0);
}

var sessionLog = profiler.SessionLogFilePath!;
var summaryPath = WpfSessionSummaryAnalyzer.TryWriteSummary(sessionLog);

Console.WriteLine($"Session log: {sessionLog}");
Console.WriteLine($"Summary: {summaryPath ?? "FAILED"}");
Console.WriteLine("Startup phase timings (ms):");
foreach (var phase in StartupPhaseRecorder.GetTimings())
    Console.WriteLine($"  {phase.Phase}: {phase.TotalMs:F1}");

return summaryPath is null ? 1 : 0;

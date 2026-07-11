using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.DependencyInjection;
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
var customers = provider.GetRequiredService<CustomerUiService>();
var purchases = provider.GetRequiredService<PurchaseUiService>();

Console.WriteLine("WPF Session Summary E2E — cloud-connected perf capture");

async Task BenchmarkSalesListAsync()
{
    using var scope = ScreenLoadProfiler.Begin("Sales.InvoiceList");
    var result = await ScreenLoadProfiler.MeasureLoadAsync(scope, () => sales.GetListAsync(null, null, 1, 100));
    scope?.IncrementServiceCalls();
    scope?.SetRowsReturned(result.Value?.Items.Count ?? 0);
}

async Task BenchmarkCustomersAsync()
{
    using var scope = ScreenLoadProfiler.Begin("Customers.List");
    var result = await ScreenLoadProfiler.MeasureLoadAsync(scope, () => customers.GetListAsync("", 1, 100));
    scope?.IncrementServiceCalls();
    scope?.SetRowsReturned(result.Value?.Items.Count ?? 0);
}

async Task BenchmarkPurchasesAsync()
{
    using var scope = ScreenLoadProfiler.Begin("Purchases.Invoices");
    var result = await ScreenLoadProfiler.MeasureLoadAsync(scope, () => purchases.GetInvoiceListAsync("", null));
    scope?.IncrementServiceCalls();
    scope?.SetRowsReturned(result.Value?.Count ?? 0);
}

async Task BenchmarkOperationsCenterAsync(Guid invoiceId)
{
    using var scope = ScreenLoadProfiler.Begin("Sales.OperationsCenter");
    var result = await ScreenLoadProfiler.MeasureLoadAsync(scope, () => sales.GetOperationsCenterAsync(invoiceId));
    scope?.IncrementServiceCalls();
    scope?.SetRowsReturned(result.IsSuccess ? 1 : 0);
}

await BenchmarkSalesListAsync();
await BenchmarkCustomersAsync();
await BenchmarkPurchasesAsync();

var listResult = await sales.GetListAsync(null, null, 1, 100);
if (listResult.IsSuccess && listResult.Value?.Items.Count > 0)
    await BenchmarkOperationsCenterAsync(listResult.Value.Items[0].Id);

var sessionLog = profiler.SessionLogFilePath;
Console.WriteLine($"Session log: {sessionLog}");

var summaryPath = WpfSessionSummaryAnalyzer.TryWriteSummary(sessionLog!);
Console.WriteLine(summaryPath is null
    ? "FAILED: no summary written"
    : $"Summary written: {summaryPath}");

return summaryPath is null ? 1 : 0;

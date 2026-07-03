using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Containers;
using ERPSystem.Application.DependencyInjection;
using ERPSystem.Application.Queries.Containers;
using ERPSystem.Application.UseCases.Containers;
using ERPSystem.Infrastructure.DependencyInjection;
using ERPSystem.Infrastructure.Seed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

static string Describe(Exception ex)
{
    var lines = new List<string>();
    for (var cur = ex; cur is not null; cur = cur.InnerException)
        lines.Add(cur.Message);
    return string.Join(" -> ", lines);
}

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var filePath = args.FirstOrDefault(a => !a.StartsWith('-'))
    ?? Path.Combine(repoRoot, "COLOMBIA.xls");
if (!File.Exists(filePath))
{
    Console.WriteLine($"File not found: {filePath}");
    return 1;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(repoRoot)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddSingleton<ICurrentUserService, TestCurrentUserService>();
services.AddSingleton<ICurrentBranchService, TestBranchService>();
services.AddSingleton<IPermissionService, AllowAllPermissionService>();
services.AddInfrastructure(configuration);
services.AddApplication();

await using var provider = services.BuildServiceProvider();
await provider.MigrateAndSeedAsync();

using var scope = provider.CreateScope();
var sp = scope.ServiceProvider;
var parseHandler = sp.GetRequiredService<ImportContainerExcelHandler>();
var bytes = await File.ReadAllBytesAsync(filePath);

var parseResult = await parseHandler.HandleAsync(new ParseContainerExcelQuery
{
    CompanyId = DatabaseSeeder.DefaultCompanyId,
    FileName = Path.GetFileName(filePath),
    FileContent = bytes
});

Console.WriteLine($"File: {Path.GetFileName(filePath)}");
if (!parseResult.IsSuccess || parseResult.Value is null)
{
    Console.WriteLine($"PARSE FAILED: {parseResult.ErrorMessage ?? "unknown"}");
    return 2;
}

var dto = parseResult.Value;
var lines = PackingListImportLineBuilder.BuildLines(dto);
var maxLot = lines.Where(l => l.LotCode is not null).Select(l => l.LotCode!.Length).DefaultIfEmpty(0).Max();
Console.WriteLine($"Parse OK: groups={dto.Groups.Count} lines={lines.Count} maxLotLen={maxLot}");

var suppliers = await sp.GetRequiredService<ERPSystem.Application.Abstractions.Repositories.ISupplierRepository>()
    .GetListAsync(DatabaseSeeder.DefaultCompanyId);
var supplierId = suppliers.FirstOrDefault()?.Supplier.Id ?? Guid.Empty;
if (supplierId == Guid.Empty)
{
    Console.WriteLine("No supplier in database.");
    return 3;
}

var containerNumber = Environment.GetEnvironmentVariable("TEST_CONTAINER_NUMBER")
    ?? $"TEST-{DateTime.UtcNow:yyyyMMddHHmmss}";
var createHandler = sp.GetRequiredService<ICommandHandler<CreateChinaContainerCommand, ERPSystem.Application.Results.ApplicationResult<Guid>>>();
var createResult = await createHandler.HandleAsync(new CreateChinaContainerCommand
{
    CompanyId = DatabaseSeeder.DefaultCompanyId,
    BranchId = DatabaseSeeder.DefaultBranchId,
    SupplierId = supplierId,
    ContainerNumber = containerNumber,
    ShipmentDate = DateTime.Today,
    ExpectedArrival = DateTime.Today.AddDays(20),
    ExchangeRateToLocalCurrency = 1m,
    ImportFileName = Path.GetFileName(filePath),
    Lines = lines
});

if (!createResult.IsSuccess)
{
    Console.WriteLine($"CREATE FAILED ({createResult.Status}): {createResult.ErrorMessage}");
    foreach (var err in createResult.ValidationErrors)
        Console.WriteLine($"  - {err.Field}: {err.Message}");
    return 4;
}

Console.WriteLine($"CREATE OK: {createResult.Value}");

var landingHandler = sp.GetRequiredService<ICommandHandler<CalculateLandingCostCommand, ERPSystem.Application.Results.ApplicationResult>>();
var landingResult = await landingHandler.HandleAsync(new CalculateLandingCostCommand
{
    ContainerId = createResult.Value,
    TotalLengthMeters = dto.GrandTotal.ParsedTotalMeters,
    ContainerWeightKg = 23500m,
    CustomsAmount = 13000m,
    Shipping = 6000m,
    Clearance = 2000m,
    OtherExpenses = 500m
});

if (!landingResult.IsSuccess)
{
    Console.WriteLine($"LANDING FAILED ({landingResult.Status}): {landingResult.ErrorMessage}");
    return 5;
}

Console.WriteLine("LANDING OK — full cost entry path succeeded.");
return 0;

sealed class TestCurrentUserService : ICurrentUserService
{
    public Guid? UserId => DatabaseSeeder.AdminUserId;
    public string? Username => "admin";
    public bool IsAuthenticated => true;
}

sealed class TestBranchService : ICurrentBranchService
{
    public Guid? CompanyId => DatabaseSeeder.DefaultCompanyId;
    public Guid? BranchId => DatabaseSeeder.DefaultBranchId;
}

sealed class AllowAllPermissionService : IPermissionService
{
    public Task<bool> CanAsync(string permissionCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task EnsureCanAsync(string permissionCode, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

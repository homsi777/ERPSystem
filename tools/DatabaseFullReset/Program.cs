using ERPSystem.Infrastructure.DependencyInjection;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Seed;
using ERPSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

var confirm = args.Any(a => a.Equals("--confirm-wipe", StringComparison.OrdinalIgnoreCase));
var autoYes = args.Any(a => a.Equals("--yes", StringComparison.OrdinalIgnoreCase));
if (!confirm)
{
    Console.WriteLine("هذا الأمر يحذف قاعدة البيانات بالكامل ويعيد إنشاءها من الصفر.");
    Console.WriteLine("Usage: dotnet run --project tools/DatabaseFullReset -- --confirm-wipe");
    return 1;
}

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

var configuration = new ConfigurationBuilder()
    .SetBasePath(repoRoot)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Local.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString(InfrastructureServiceCollectionExtensions.ConnectionStringName)
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");

var builder = new NpgsqlConnectionStringBuilder(connectionString);
Console.WriteLine();
Console.WriteLine("=== تنبيه: حذف كامل لقاعدة البيانات ===");
Console.WriteLine($"Host: {builder.Host}:{builder.Port}");
Console.WriteLine($"Database: {builder.Database}");
Console.WriteLine($"User: {builder.Username}");
Console.WriteLine();
if (!autoYes)
{
    Console.Write("اكتب نعم للمتابعة: ");
    var answer = Console.ReadLine()?.Trim();
    if (!string.Equals(answer, "نعم", StringComparison.Ordinal))
    {
        Console.WriteLine("تم الإلغاء.");
        return 1;
    }
}

using var tunnel = SshTunnelService.StartIfConfigured(configuration);

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddInfrastructure(configuration);

await using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

await FullDatabaseReset.RecreateAndSeedAsync(
    context,
    logger,
    () => provider.MigrateAndSeedAsync());

Console.WriteLine();
Console.WriteLine("تم التنظيف الشامل. النظام جاهز لتجربة جديدة.");
Console.WriteLine($"المستخدم: admin");
Console.WriteLine($"كلمة المرور: {DatabaseSeeder.DefaultAdminPassword}");
return 0;

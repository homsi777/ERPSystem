using ERPSystem.Infrastructure.Persistence;

namespace ERPSystem.Infrastructure.Seed;

internal static class CapitalModuleSeeder
{
    public static Task EnsureAsync(
        ErpDbContext context,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

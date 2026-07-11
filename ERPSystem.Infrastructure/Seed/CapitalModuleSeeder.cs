using ERPSystem.Infrastructure.Persistence;

namespace ERPSystem.Infrastructure.Seed;

internal static class CapitalModuleSeeder
{
    public static async Task EnsureAsync(
        ErpDbContext context,
        Guid adminRoleId,
        CancellationToken cancellationToken = default)
    {
        await DatabaseSeeder.EnsurePermissionsAsync(context,
        [
            ("capital.view", "capital", "view"),
            ("capital.create", "capital", "create"),
            ("capital.edit", "capital", "edit"),
            ("capital.delete", "capital", "delete"),
            ("capital.approve", "capital", "approve"),
            ("capital.export", "capital", "export"),
            ("capital.print", "capital", "print"),
            ("capital.archive", "capital", "archive")
        ], cancellationToken);
    }
}

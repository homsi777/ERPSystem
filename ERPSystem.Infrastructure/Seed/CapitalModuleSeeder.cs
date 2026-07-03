using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Seed;

internal static class CapitalModuleSeeder
{
    public static async Task EnsureAsync(
        ErpDbContext context,
        Guid adminRoleId,
        CancellationToken cancellationToken = default)
    {
        await EnsurePermissionsAsync(context, adminRoleId, cancellationToken);
    }

    private static async Task EnsurePermissionsAsync(
        ErpDbContext context,
        Guid adminRoleId,
        CancellationToken cancellationToken)
    {
        var codes = new[]
        {
            ("capital.view", "capital", "view"),
            ("capital.create", "capital", "create"),
            ("capital.edit", "capital", "edit"),
            ("capital.delete", "capital", "delete"),
            ("capital.approve", "capital", "approve"),
            ("capital.export", "capital", "export"),
            ("capital.print", "capital", "print"),
            ("capital.archive", "capital", "archive")
        };

        foreach (var (code, module, action) in codes)
        {
            var permission = await context.Permissions
                .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);

            if (permission is null)
            {
                permission = new PermissionEntity
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Module = module,
                    Action = action
                };
                context.Permissions.Add(permission);
            }

            if (!await context.RolePermissions.AnyAsync(
                    rp => rp.RoleId == adminRoleId && rp.PermissionId == permission.Id,
                    cancellationToken))
            {
                context.RolePermissions.Add(new RolePermissionEntity
                {
                    RoleId = adminRoleId,
                    PermissionId = permission.Id
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

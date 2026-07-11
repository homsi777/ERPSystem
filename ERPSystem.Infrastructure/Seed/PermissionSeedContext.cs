using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Seed;

/// <summary>Shared in-memory permission state for idempotent seeding — one DB read pass for all modules.</summary>
internal sealed class PermissionSeedContext
{
    private readonly Guid _adminRoleId;
    private readonly Dictionary<string, Guid> _existingPermissions;
    private readonly bool _adminRoleExists;
    private readonly HashSet<Guid> _linkedPermissionIds;

    private PermissionSeedContext(
        Guid adminRoleId,
        Dictionary<string, Guid> existingPermissions,
        bool adminRoleExists,
        HashSet<Guid> linkedPermissionIds)
    {
        _adminRoleId = adminRoleId;
        _existingPermissions = existingPermissions;
        _adminRoleExists = adminRoleExists;
        _linkedPermissionIds = linkedPermissionIds;
    }

    public static async Task<PermissionSeedContext> LoadAsync(
        ErpDbContext context,
        IReadOnlyCollection<string> allSeedCodes,
        Guid adminRoleId,
        CancellationToken cancellationToken)
    {
        var codes = allSeedCodes.Distinct().ToArray();
        var existingPermissions = codes.Length == 0
            ? new Dictionary<string, Guid>()
            : await context.Permissions.AsNoTracking()
                .Where(p => codes.Contains(p.Code))
                .ToDictionaryAsync(p => p.Code, p => p.Id, cancellationToken);

        var adminRoleExists = await context.Roles.AsNoTracking()
            .AnyAsync(r => r.Id == adminRoleId, cancellationToken);

        var linkedPermissionIds = adminRoleExists
            ? await context.RolePermissions.AsNoTracking()
                .Where(rp => rp.RoleId == adminRoleId)
                .Select(rp => rp.PermissionId)
                .ToHashSetAsync(cancellationToken)
            : [];

        return new PermissionSeedContext(adminRoleId, existingPermissions, adminRoleExists, linkedPermissionIds);
    }

    public void ApplyPermissions(
        ErpDbContext context,
        IEnumerable<(string Code, string Module, string Action)> permissions)
    {
        foreach (var (code, module, action) in permissions)
        {
            if (!_existingPermissions.TryGetValue(code, out var permissionId))
            {
                permissionId = Guid.NewGuid();
                context.Permissions.Add(new PermissionEntity
                {
                    Id = permissionId,
                    Code = code,
                    Module = module,
                    Action = action
                });
                _existingPermissions[code] = permissionId;
            }

            if (_adminRoleExists && _linkedPermissionIds.Add(permissionId))
            {
                context.RolePermissions.Add(new RolePermissionEntity
                {
                    RoleId = _adminRoleId,
                    PermissionId = permissionId
                });
            }
        }
    }
}

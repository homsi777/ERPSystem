using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Identity;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Identity;
using ERPSystem.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class IdentityAdminRepository(ErpDbContext context) : IIdentityAdminRepository
{
    public async Task<IReadOnlyList<IdentityUserListDto>> GetVisibleUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await context.Users.AsNoTracking()
            .Where(u => u.Id != IdentityHiddenAccounts.RootUserId)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();
        var roleLinks = await context.UserRoles.AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId))
            .ToListAsync(cancellationToken);
        var roleIds = roleLinks.Select(ur => ur.RoleId).Distinct().ToList();
        var roles = await context.Roles.AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        return users.Select(u =>
        {
            var names = roleLinks
                .Where(ur => ur.UserId == u.Id)
                .Select(ur => roles.TryGetValue(ur.RoleId, out var role) ? role.Name : "")
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            return new IdentityUserListDto
            {
                Id = u.Id,
                Username = u.Username,
                FullNameAr = u.FullNameAr,
                FullNameEn = u.FullNameEn,
                IsActive = u.IsActive,
                RoleNames = names
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<IdentityRoleListDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await context.Roles.AsNoTracking()
            .OrderByDescending(r => r.IsSystem)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var roleIds = roles.Select(r => r.Id).ToList();
        var counts = await context.RolePermissions.AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .GroupBy(rp => rp.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        var totalPermissions = await context.Permissions.AsNoTracking().CountAsync(cancellationToken);

        return roles.Select(r => new IdentityRoleListDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsSystem = r.IsSystem,
            PermissionCount = r.IsSystem ? totalPermissions : counts.GetValueOrDefault(r.Id)
        }).ToList();
    }

    public async Task<IReadOnlyList<PermissionModuleGroupDto>> GetPermissionTreeAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await context.Permissions.AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Code)
            .ToListAsync(cancellationToken);

        return permissions
            .Where(p => PermissionModuleCatalog.IsAssignableModule(p.Module))
            .GroupBy(p => p.Module)
            .OrderBy(g => PermissionModuleCatalog.GetModuleSortOrder(g.Key))
            .Select(g => new PermissionModuleGroupDto
            {
                ModuleKey = g.Key,
                ModuleLabelAr = PermissionDisplayCatalog.GetModuleLabel(g.Key),
                Permissions = g.Select(p => new PermissionTreeItemDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    LabelAr = PermissionDisplayCatalog.GetPermissionLabel(p.Code, p.Module, p.Action)
                }).ToList()
            })
            .ToList();
    }

    public async Task<RolePermissionsDto?> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        if (role is null)
            return null;

        if (role.IsSystem)
        {
            var allCodes = await context.Permissions.AsNoTracking()
                .Select(p => p.Code)
                .OrderBy(c => c)
                .ToListAsync(cancellationToken);

            return new RolePermissionsDto
            {
                RoleId = role.Id,
                RoleName = role.Name,
                IsSystem = true,
                PermissionCodes = allCodes
            };
        }

        var permissionIds = await context.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        var codes = await context.Permissions.AsNoTracking()
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Code)
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);

        return new RolePermissionsDto
        {
            RoleId = role.Id,
            RoleName = role.Name,
            IsSystem = false,
            PermissionCodes = codes
        };
    }

    public async Task ReplaceRolePermissionsAsync(
        Guid roleId,
        IReadOnlyList<string> permissionCodes,
        CancellationToken cancellationToken = default)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        if (role.IsSystem)
            throw new InvalidOperationException("Cannot modify permissions for a system role.");

        var normalizedCodes = permissionCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var permissionMap = await context.Permissions.AsNoTracking()
            .Where(p => normalizedCodes.Contains(p.Code))
            .ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var unknown = normalizedCodes.Where(c => !permissionMap.ContainsKey(c)).ToList();
        if (unknown.Count > 0)
            throw new InvalidOperationException($"Unknown permission codes: {string.Join(", ", unknown)}");

        var existing = await context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);
        context.RolePermissions.RemoveRange(existing);

        foreach (var code in normalizedCodes)
        {
            context.RolePermissions.Add(new RolePermissionEntity
            {
                RoleId = roleId,
                PermissionId = permissionMap[code]
            });
        }
    }

    public async Task<Guid> CreateRoleAsync(string name, string description, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new InvalidOperationException("Role name is required.");

        if (await context.Roles.AnyAsync(r => r.Name == trimmed, cancellationToken))
            throw new InvalidOperationException("Role name already exists.");

        var role = new RoleEntity
        {
            Id = Guid.NewGuid(),
            Name = trimmed,
            Description = description.Trim(),
            IsSystem = false,
            IsActive = true
        };
        context.Roles.Add(role);
        return role.Id;
    }

    public async Task<Guid> CreateUserAsync(
        string username,
        string passwordHash,
        string fullNameAr,
        string fullNameEn,
        CancellationToken cancellationToken = default)
    {
        var trimmedUsername = username.Trim();
        if (string.IsNullOrWhiteSpace(trimmedUsername))
            throw new InvalidOperationException("Username is required.");

        if (IdentityHiddenAccounts.IsHidden(trimmedUsername))
            throw new InvalidOperationException("Username is reserved.");

        if (await context.Users.AnyAsync(u => u.Username == trimmedUsername, cancellationToken))
            throw new InvalidOperationException("Username already exists.");

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Username = trimmedUsername,
            PasswordHash = passwordHash,
            FullNameAr = fullNameAr.Trim(),
            FullNameEn = fullNameEn.Trim(),
            IsActive = true
        };
        context.Users.Add(user);
        return user.Id;
    }

    public async Task SetUserRolesAsync(
        Guid userId,
        IReadOnlyList<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        if (userId == IdentityHiddenAccounts.RootUserId)
            throw new InvalidOperationException("Cannot modify this user.");

        var existing = await context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync(cancellationToken);
        context.UserRoles.RemoveRange(existing);

        foreach (var roleId in roleIds.Distinct())
        {
            context.UserRoles.Add(new UserRoleEntity { UserId = userId, RoleId = roleId });
        }
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default) =>
        context.Users.AsNoTracking()
            .AnyAsync(u => u.Username == username.Trim(), cancellationToken);
}

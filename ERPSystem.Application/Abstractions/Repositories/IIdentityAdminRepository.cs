using ERPSystem.Application.DTOs.Identity;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IIdentityAdminRepository
{
    Task<IReadOnlyList<IdentityUserListDto>> GetVisibleUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdentityRoleListDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionModuleGroupDto>> GetPermissionTreeAsync(CancellationToken cancellationToken = default);
    Task<RolePermissionsDto?> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task ReplaceRolePermissionsAsync(Guid roleId, IReadOnlyList<string> permissionCodes, CancellationToken cancellationToken = default);
    Task<Guid> CreateRoleAsync(string name, string description, CancellationToken cancellationToken = default);
    Task<Guid> CreateUserAsync(string username, string passwordHash, string fullNameAr, string fullNameEn, CancellationToken cancellationToken = default);
    Task SetUserRolesAsync(Guid userId, IReadOnlyList<Guid> roleIds, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
}

using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Identity;

namespace ERPSystem.Application.Queries.Identity;

public sealed class GetIdentityUsersQuery;

public sealed class GetIdentityRolesQuery;

public sealed class GetPermissionTreeQuery;

public sealed class GetRolePermissionsQuery
{
    public Guid RoleId { get; init; }
}

public sealed class GetIdentityUsersHandler(IIdentityAdminRepository repository)
{
    public Task<IReadOnlyList<IdentityUserListDto>> HandleAsync(
        GetIdentityUsersQuery query,
        CancellationToken cancellationToken = default) =>
        repository.GetVisibleUsersAsync(cancellationToken);
}

public sealed class GetIdentityRolesHandler(IIdentityAdminRepository repository)
{
    public Task<IReadOnlyList<IdentityRoleListDto>> HandleAsync(
        GetIdentityRolesQuery query,
        CancellationToken cancellationToken = default) =>
        repository.GetRolesAsync(cancellationToken);
}

public sealed class GetPermissionTreeHandler(IIdentityAdminRepository repository)
{
    public Task<IReadOnlyList<PermissionModuleGroupDto>> HandleAsync(
        GetPermissionTreeQuery query,
        CancellationToken cancellationToken = default) =>
        repository.GetPermissionTreeAsync(cancellationToken);
}

public sealed class GetRolePermissionsHandler(IIdentityAdminRepository repository)
{
    public Task<RolePermissionsDto?> HandleAsync(
        GetRolePermissionsQuery query,
        CancellationToken cancellationToken = default) =>
        repository.GetRolePermissionsAsync(query.RoleId, cancellationToken);
}

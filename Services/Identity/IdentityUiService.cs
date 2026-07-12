using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Commands.Identity;
using ERPSystem.Application.DTOs.Identity;
using ERPSystem.Application.Queries.Identity;
using ERPSystem.Application.Results;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Identity;

public sealed class IdentityUiService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public IdentityUiService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public static IdentityUiService Instance => AppServices.GetRequiredService<IdentityUiService>();

    public async Task<IReadOnlyList<IdentityUserListDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetIdentityUsersHandler>();
        return await handler.HandleAsync(new GetIdentityUsersQuery(), cancellationToken);
    }

    public async Task<IReadOnlyList<IdentityRoleListDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetIdentityRolesHandler>();
        return await handler.HandleAsync(new GetIdentityRolesQuery(), cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionModuleGroupDto>> GetPermissionTreeAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetPermissionTreeHandler>();
        return await handler.HandleAsync(new GetPermissionTreeQuery(), cancellationToken);
    }

    public async Task<RolePermissionsDto?> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRolePermissionsHandler>();
        return await handler.HandleAsync(new GetRolePermissionsQuery { RoleId = roleId }, cancellationToken);
    }

    public async Task<ApplicationResult> SaveRolePermissionsAsync(
        Guid roleId,
        IReadOnlyList<string> permissionCodes,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateRolePermissionsCommand, ApplicationResult>>();
        return await handler.HandleAsync(new UpdateRolePermissionsCommand
        {
            RoleId = roleId,
            PermissionCodes = permissionCodes
        }, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateRoleAsync(
        string name,
        string description,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateIdentityRoleCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreateIdentityRoleCommand
        {
            Name = name,
            Description = description
        }, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateUserAsync(
        string username,
        string password,
        string fullNameAr,
        string fullNameEn,
        IReadOnlyList<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateIdentityUserCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreateIdentityUserCommand
        {
            Username = username,
            Password = password,
            FullNameAr = fullNameAr,
            FullNameEn = fullNameEn,
            RoleIds = roleIds
        }, cancellationToken);
    }
}

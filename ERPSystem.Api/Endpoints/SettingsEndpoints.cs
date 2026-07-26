using ERPSystem.Api.Mapping;
using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Identity;
using ERPSystem.Application.Common;
using ERPSystem.Application.Queries.Identity;
using ERPSystem.Application.Results;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Api.Endpoints;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/settings")
            .RequireAuthorization();

        group.MapGet("/user-sessions", GetUserSessionsAsync)
            .WithName("SettingsUserSessions");
        group.MapGet("/users", GetUsersAsync)
            .WithName("SettingsUsers");
        group.MapPost("/users", CreateUserAsync)
            .WithName("SettingsCreateUser");
        group.MapPut("/users/{userId:guid}/roles", UpdateUserRolesAsync)
            .WithName("SettingsUpdateUserRoles");
        group.MapGet("/roles", GetRolesAsync)
            .WithName("SettingsRoles");
        group.MapPost("/roles", CreateRoleAsync)
            .WithName("SettingsCreateRole");
        group.MapGet("/permissions", GetPermissionTreeAsync)
            .WithName("SettingsPermissions");
        group.MapGet("/roles/{roleId:guid}/permissions", GetRolePermissionsAsync)
            .WithName("SettingsRolePermissions");
        group.MapPut("/roles/{roleId:guid}/permissions", UpdateRolePermissionsAsync)
            .WithName("SettingsUpdateRolePermissions");

        return app;
    }

    private static async Task<IResult> GetUserSessionsAsync(
        IUserSessionRepository userSessionRepository,
        IPermissionService permissionService,
        [FromQuery] int limit = 200,
        CancellationToken cancellationToken = default)
    {
        var denied = await EnsureGeneralManagerAsync(permissionService, cancellationToken);
        if (denied is not null)
            return denied;

        var rows = await userSessionRepository.GetHistoryAsync(limit, cancellationToken);
        return Results.Ok(rows);
    }

    private static async Task<IResult> GetUsersAsync(
        GetIdentityUsersHandler handler,
        IPermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureGeneralManagerAsync(permissionService, cancellationToken);
        if (denied is not null)
            return denied;

        var rows = await handler.HandleAsync(new GetIdentityUsersQuery(), cancellationToken);
        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateUserAsync(
        [FromBody] CreateIdentityUserRequest request,
        ICommandHandler<CreateIdentityUserCommand, ApplicationResult<Guid>> handler,
        IPermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureGeneralManagerAsync(permissionService, cancellationToken);
        if (denied is not null)
            return denied;

        var result = await handler.HandleAsync(new CreateIdentityUserCommand
        {
            Username = request.Username,
            Password = request.Password,
            FullNameAr = request.FullNameAr,
            FullNameEn = request.FullNameEn,
            RoleIds = request.RoleIds ?? []
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result, id => Results.Ok(new { id }));
    }

    private static async Task<IResult> UpdateUserRolesAsync(
        Guid userId,
        [FromBody] UpdateIdentityUserRolesRequest request,
        ICommandHandler<UpdateIdentityUserRolesCommand, ApplicationResult> handler,
        IPermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureGeneralManagerAsync(permissionService, cancellationToken);
        if (denied is not null)
            return denied;

        var result = await handler.HandleAsync(new UpdateIdentityUserRolesCommand
        {
            UserId = userId,
            RoleIds = request.RoleIds ?? []
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetRolesAsync(
        GetIdentityRolesHandler handler,
        IPermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureGeneralManagerAsync(permissionService, cancellationToken);
        if (denied is not null)
            return denied;

        var rows = await handler.HandleAsync(new GetIdentityRolesQuery(), cancellationToken);
        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateRoleAsync(
        [FromBody] CreateIdentityRoleRequest request,
        ICommandHandler<CreateIdentityRoleCommand, ApplicationResult<Guid>> handler,
        IPermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureGeneralManagerAsync(permissionService, cancellationToken);
        if (denied is not null)
            return denied;

        var result = await handler.HandleAsync(new CreateIdentityRoleCommand
        {
            Name = request.Name,
            Description = request.Description
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result, id => Results.Ok(new { id }));
    }

    private static async Task<IResult> GetPermissionTreeAsync(
        GetPermissionTreeHandler handler,
        IPermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureGeneralManagerAsync(permissionService, cancellationToken);
        if (denied is not null)
            return denied;

        var rows = await handler.HandleAsync(new GetPermissionTreeQuery(), cancellationToken);
        return Results.Ok(rows);
    }

    private static async Task<IResult> GetRolePermissionsAsync(
        Guid roleId,
        GetRolePermissionsHandler handler,
        IPermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureGeneralManagerAsync(permissionService, cancellationToken);
        if (denied is not null)
            return denied;

        var result = await handler.HandleAsync(
            new GetRolePermissionsQuery { RoleId = roleId },
            cancellationToken);

        return result is null
            ? Results.Json(
                new ApiErrorResponse("NotFound", "الدور غير موجود.", []),
                statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(result);
    }

    private static async Task<IResult> UpdateRolePermissionsAsync(
        Guid roleId,
        [FromBody] UpdateRolePermissionsRequest request,
        ICommandHandler<UpdateRolePermissionsCommand, ApplicationResult> handler,
        IPermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureGeneralManagerAsync(permissionService, cancellationToken);
        if (denied is not null)
            return denied;

        var result = await handler.HandleAsync(new UpdateRolePermissionsCommand
        {
            RoleId = roleId,
            PermissionCodes = request.PermissionCodes ?? []
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult?> EnsureGeneralManagerAsync(
        IPermissionService permissionService,
        CancellationToken cancellationToken)
    {
        if (await permissionService.CanAsync(GeneralManagerAccess.PermissionCode, cancellationToken))
            return null;

        return Results.Json(
            new ApiErrorResponse(
                "PermissionDenied",
                "هذا القسم متاح لحساب المدير العام فقط.",
                []),
            statusCode: StatusCodes.Status403Forbidden);
    }

    private sealed record CreateIdentityUserRequest(
        string Username,
        string Password,
        string FullNameAr,
        string FullNameEn,
        IReadOnlyList<Guid>? RoleIds);

    private sealed record UpdateIdentityUserRolesRequest(IReadOnlyList<Guid>? RoleIds);

    private sealed record CreateIdentityRoleRequest(string Name, string Description);

    private sealed record UpdateRolePermissionsRequest(IReadOnlyList<string>? PermissionCodes);
}

using ERPSystem.Api.Auth;
using ERPSystem.Api.Contracts;
using ERPSystem.Api.Mapping;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.DTOs.Identity;
using ERPSystem.Application.Queries.Identity;
using ERPSystem.Application.UseCases.Identity;
using ERPSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ERPSystem.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithName("AuthLogin");

        group.MapPost("/refresh", RefreshAsync)
            .AllowAnonymous()
            .WithName("AuthRefresh");

        group.MapPost("/logout", LogoutAsync)
            .AllowAnonymous()
            .WithName("AuthLogout");

        group.MapGet("/me", MeAsync)
            .RequireAuthorization()
            .WithName("AuthMe");

        return app;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext httpContext,
        [FromBody] LoginRequest request,
        AuthenticateUserHandler handler,
        IJwtTokenService jwtTokenService,
        IUserSessionRepository userSessionRepository,
        IOptions<JwtSettings> jwtOptions,
        CancellationToken cancellationToken)
    {
        var authResult = await handler.HandleAsync(new AuthenticateUserQuery
        {
            Username = request.Username,
            Password = request.Password
        }, cancellationToken);

        if (!authResult.IsSuccess)
            return ApplicationResultHttpMapper.ToUnauthorized(authResult);

        var user = authResult.Value!;
        var refreshToken = jwtTokenService.CreateRefreshToken();
        var refreshExpiresAt = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays);
        var deviceInfo = httpContext.Request.Headers.UserAgent.ToString();
        if (!string.IsNullOrWhiteSpace(request.ClientType))
            deviceInfo = $"{request.ClientType} | {deviceInfo}";

        var session = await userSessionRepository.StartSessionAsync(
            user.UserId,
            user.Username,
            user.FullNameAr,
            UserSessionClientType.Web,
            refreshToken,
            string.IsNullOrWhiteSpace(deviceInfo) ? "متصفح ويب" : deviceInfo,
            httpContext.Connection.RemoteIpAddress?.ToString(),
            refreshExpiresAt,
            cancellationToken);

        var (accessToken, accessExpiresAt) = jwtTokenService.CreateAccessToken(user, session.SessionId);

        return Results.Ok(new AuthTokenResponse(
            accessToken,
            refreshToken,
            accessExpiresAt,
            refreshExpiresAt,
            user));
    }

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshTokenRequest request,
        IJwtTokenService jwtTokenService,
        IRefreshTokenStore refreshTokenStore,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Results.Json(
                new ApiErrorResponse("ValidationFailed", "Refresh token is required.", []),
                statusCode: StatusCodes.Status400BadRequest);

        var stored = await refreshTokenStore.GetAsync(request.RefreshToken, cancellationToken);
        if (stored is null)
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Invalid or expired refresh token.", []),
                statusCode: StatusCodes.Status401Unauthorized);

        var roles = await userRepository.GetRolesForUserAsync(stored.UserId, cancellationToken);
        var permissions = await userRepository.GetPermissionCodesForUserAsync(stored.UserId, cancellationToken);

        var user = new AuthenticatedUserDto
        {
            UserId = stored.UserId,
            Username = stored.Username,
            FullNameAr = stored.FullNameAr,
            Roles = roles.Select(r => r.Name).ToList(),
            Permissions = permissions
        };

        var (accessToken, accessExpiresAt) = jwtTokenService.CreateAccessToken(user, stored.SessionId);
        return Results.Ok(new RefreshTokenResponse(accessToken, accessExpiresAt));
    }

    private static async Task<IResult> LogoutAsync(
        [FromBody] LogoutRequest request,
        IRefreshTokenStore refreshTokenStore,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            await refreshTokenStore.RevokeAsync(request.RefreshToken, cancellationToken);

        return Results.Ok(new { message = "Logged out." });
    }

    private static async Task<IResult> MeAsync(
        ICurrentUserService currentUser,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Results.Json(
                new ApiErrorResponse("NotFound", "User not found.", []),
                statusCode: StatusCodes.Status404NotFound);

        var roles = await userRepository.GetRolesForUserAsync(userId, cancellationToken);
        var permissions = await userRepository.GetPermissionCodesForUserAsync(userId, cancellationToken);

        return Results.Ok(new MeResponse(
            user.Id,
            user.Username,
            user.FullNameAr,
            roles.Select(r => r.Name).ToList(),
            permissions));
    }
}

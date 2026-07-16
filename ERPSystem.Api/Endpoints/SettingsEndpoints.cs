using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Identity;
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

        return app;
    }

    private static async Task<IResult> GetUserSessionsAsync(
        IUserSessionRepository userSessionRepository,
        [FromQuery] int limit = 200,
        CancellationToken cancellationToken = default)
    {
        var rows = await userSessionRepository.GetHistoryAsync(limit, cancellationToken);
        return Results.Ok(rows);
    }
}

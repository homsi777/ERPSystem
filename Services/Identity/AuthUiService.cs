using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Identity;
using ERPSystem.Application.Queries.Identity;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Identity;
using ERPSystem.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Identity;

public sealed class AuthUiService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    public AuthUiService(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    public static AuthUiService Instance => AppServices.GetRequiredService<AuthUiService>();

    public async Task<ApplicationResult<(AuthenticatedUserDto User, Guid SessionId)>> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<AuthenticateUserHandler>();
        var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();

        var authResult = await handler.HandleAsync(new AuthenticateUserQuery
        {
            Username = username,
            Password = password
        }, cancellationToken);

        if (!authResult.IsSuccess || authResult.Value is null)
            return ApplicationResult<(AuthenticatedUserDto, Guid)>.Failure(authResult.ErrorMessage ?? "فشل تسجيل الدخول.");

        var user = authResult.Value;
        var refreshDays = _configuration.GetValue("Jwt:RefreshTokenDays", 7);
        var expiresAt = DateTime.UtcNow.AddDays(refreshDays);

        var session = await sessions.StartSessionAsync(
            user.UserId,
            user.Username,
            user.FullNameAr,
            UserSessionClientType.Desktop,
            refreshTokenPlain: null,
            deviceInfo: Environment.MachineName,
            ipAddress: null,
            expiresAt,
            cancellationToken);

        return ApplicationResult<(AuthenticatedUserDto, Guid)>.Success((user, session.SessionId));
    }

    public async Task EndSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            return;

        using var scope = _scopeFactory.CreateScope();
        var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
        await sessions.EndDesktopSessionAsync(sessionId, cancellationToken);
    }

    public async Task<bool> IsSessionActiveAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            return false;

        using var scope = _scopeFactory.CreateScope();
        var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
        return await sessions.IsSessionActiveAsync(sessionId, cancellationToken);
    }
}

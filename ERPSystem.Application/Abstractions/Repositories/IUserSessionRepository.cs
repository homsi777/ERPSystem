using ERPSystem.Application.DTOs.Identity;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IUserSessionRepository
{
    Task<StartUserSessionResult> StartSessionAsync(
        Guid userId,
        string username,
        string fullNameAr,
        UserSessionClientType clientType,
        string? refreshTokenPlain,
        string? deviceInfo,
        string? ipAddress,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);

    Task<(Guid SessionId, Guid UserId, string Username, string FullNameAr, DateTime ExpiresAt)?> GetByRefreshTokenAsync(
        string refreshTokenPlain,
        CancellationToken cancellationToken = default);

    Task RevokeByRefreshTokenAsync(string refreshTokenPlain, string reason, CancellationToken cancellationToken = default);

    Task<bool> IsSessionActiveAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task TouchSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task EndDesktopSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSessionStatusDto>> GetHistoryAsync(
        int limit,
        CancellationToken cancellationToken = default);
}

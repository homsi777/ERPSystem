using System.Security.Cryptography;
using System.Text;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Identity;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class UserSessionRepository(ErpDbContext context) : IUserSessionRepository
{
    public async Task<StartUserSessionResult> StartSessionAsync(
        Guid userId,
        string username,
        string fullNameAr,
        UserSessionClientType clientType,
        string? refreshTokenPlain,
        string? deviceInfo,
        string? ipAddress,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var activeSessions = await context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.LogoutAt == null && s.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.IsRevoked = true;
            session.RevokedReason = "NewLogin";
            session.LogoutAt = now;
        }

        var sessionId = Guid.NewGuid();
        var entity = new UserSessionEntity
        {
            Id = sessionId,
            UserId = userId,
            Username = username,
            FullNameAr = fullNameAr,
            ClientType = (int)clientType,
            RefreshTokenHash = string.IsNullOrWhiteSpace(refreshTokenPlain)
                ? null
                : HashToken(refreshTokenPlain),
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            LoginAt = now,
            LastSeenAt = now,
            ExpiresAt = expiresAt,
            IsRevoked = false
        };

        await context.UserSessions.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return new StartUserSessionResult
        {
            SessionId = sessionId,
            RefreshToken = refreshTokenPlain,
            ExpiresAt = expiresAt
        };
    }

    public async Task<(Guid SessionId, Guid UserId, string Username, string FullNameAr, DateTime ExpiresAt)?> GetByRefreshTokenAsync(
        string refreshTokenPlain,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenPlain))
            return null;

        var hash = HashToken(refreshTokenPlain);
        var now = DateTime.UtcNow;

        var session = await context.UserSessions.AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.RefreshTokenHash == hash
                && !s.IsRevoked
                && s.LogoutAt == null
                && s.ExpiresAt > now,
                cancellationToken);

        return session is null
            ? null
            : (session.Id, session.UserId, session.Username, session.FullNameAr, session.ExpiresAt);
    }

    public async Task RevokeByRefreshTokenAsync(
        string refreshTokenPlain,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenPlain))
            return;

        var hash = HashToken(refreshTokenPlain);
        var session = await context.UserSessions
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == hash && !s.IsRevoked, cancellationToken);

        if (session is null)
            return;

        session.IsRevoked = true;
        session.RevokedReason = reason;
        session.LogoutAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsSessionActiveAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            return false;

        var now = DateTime.UtcNow;
        return await context.UserSessions.AsNoTracking()
            .AnyAsync(s =>
                s.Id == sessionId
                && !s.IsRevoked
                && s.LogoutAt == null
                && s.ExpiresAt > now,
                cancellationToken);
    }

    public async Task TouchSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            return;

        var session = await context.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsRevoked, cancellationToken);

        if (session is null)
            return;

        session.LastSeenAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task EndDesktopSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            return;

        var session = await context.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsRevoked, cancellationToken);

        if (session is null)
            return;

        session.IsRevoked = true;
        session.RevokedReason = "Logout";
        session.LogoutAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserSessionStatusDto>> GetHistoryAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 500);
        var now = DateTime.UtcNow;

        var rows = await context.UserSessions.AsNoTracking()
            .OrderByDescending(s => s.LoginAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    private static UserSessionStatusDto Map(UserSessionEntity session)
    {
        var clientType = (UserSessionClientType)session.ClientType;
        var isActive = !session.IsRevoked && session.LogoutAt == null && session.ExpiresAt > DateTime.UtcNow;

        return new UserSessionStatusDto
        {
            Id = session.Id,
            UserId = session.UserId,
            Username = session.Username,
            FullNameAr = session.FullNameAr,
            ClientType = clientType,
            ClientTypeDisplay = clientType == UserSessionClientType.Web ? "متصفح ويب" : "تطبيق سطح مكتب",
            DeviceInfo = session.DeviceInfo,
            IpAddress = session.IpAddress,
            LoginAt = session.LoginAt,
            LogoutAt = session.LogoutAt,
            LastSeenAt = session.LastSeenAt,
            IsActive = isActive,
            StatusDisplay = isActive ? "نشط" : session.LogoutAt.HasValue ? "خروج" : "منتهٍ"
        };
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}

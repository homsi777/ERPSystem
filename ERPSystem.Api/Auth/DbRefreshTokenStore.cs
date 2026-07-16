using ERPSystem.Application.Abstractions.Repositories;

namespace ERPSystem.Api.Auth;

internal sealed class DbRefreshTokenStore(IUserSessionRepository sessions) : IRefreshTokenStore
{
    public Task StoreAsync(string refreshToken, RefreshTokenRecord record, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Use IUserSessionRepository.StartSessionAsync for login.");

    public async Task<RefreshTokenRecord?> GetAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var stored = await sessions.GetByRefreshTokenAsync(refreshToken, cancellationToken);
        return stored is null
            ? null
            : new RefreshTokenRecord(
                stored.Value.SessionId,
                stored.Value.UserId,
                stored.Value.Username,
                stored.Value.FullNameAr,
                stored.Value.ExpiresAt);
    }

    public Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
        => sessions.RevokeByRefreshTokenAsync(refreshToken, "Logout", cancellationToken);
}

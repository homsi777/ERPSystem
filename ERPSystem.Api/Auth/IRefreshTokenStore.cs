namespace ERPSystem.Api.Auth;

public sealed record RefreshTokenRecord(
    Guid UserId,
    string Username,
    string FullNameAr,
    DateTime ExpiresAt);

public interface IRefreshTokenStore
{
    Task StoreAsync(string refreshToken, RefreshTokenRecord record, CancellationToken cancellationToken = default);
    Task<RefreshTokenRecord?> GetAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}

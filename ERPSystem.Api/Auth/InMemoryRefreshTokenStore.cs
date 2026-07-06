using System.Collections.Concurrent;

namespace ERPSystem.Api.Auth;

internal sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly ConcurrentDictionary<string, RefreshTokenRecord> _tokens = new();

    public Task StoreAsync(string refreshToken, RefreshTokenRecord record, CancellationToken cancellationToken = default)
    {
        _tokens[refreshToken] = record;
        return Task.CompletedTask;
    }

    public Task<RefreshTokenRecord?> GetAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (!_tokens.TryGetValue(refreshToken, out var record))
            return Task.FromResult<RefreshTokenRecord?>(null);

        if (record.ExpiresAt <= DateTime.UtcNow)
        {
            _tokens.TryRemove(refreshToken, out _);
            return Task.FromResult<RefreshTokenRecord?>(null);
        }

        return Task.FromResult<RefreshTokenRecord?>(record);
    }

    public Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        _tokens.TryRemove(refreshToken, out _);
        return Task.CompletedTask;
    }
}

using System.Collections.Concurrent;
using ERPSystem.Application.Abstractions.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Infrastructure.Services;

internal sealed class InMemoryCacheService(
    IMemoryCache memoryCache,
    ILogger<InMemoryCacheService> logger) : ICacheService
{
    private static readonly ConcurrentDictionary<string, byte> Keys = new(StringComparer.Ordinal);
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var found = memoryCache.TryGetValue(key, out T? value);
        logger.LogDebug("Cache {Result}: {Key}", found ? "HIT" : "MISS", key);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        memoryCache.Set(
            key,
            value,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
            });
        Keys.TryAdd(key, 0);
        logger.LogDebug("Cache SET: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        memoryCache.Remove(key);
        Keys.TryRemove(key, out _);
        logger.LogDebug("Cache REMOVE: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        foreach (var key in Keys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
        {
            memoryCache.Remove(key);
            Keys.TryRemove(key, out _);
        }

        logger.LogDebug("Cache REMOVE PREFIX: {Prefix}", prefix);
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        foreach (var key in Keys.Keys.ToList())
        {
            memoryCache.Remove(key);
            Keys.TryRemove(key, out _);
        }

        logger.LogInformation("Cache CLEARED");
        return Task.CompletedTask;
    }
}

using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Payment.Application.Common.Interfaces;

namespace Payment.Infrastructure.Caching;

public sealed class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;

    public InMemoryCacheService(IMemoryCache memoryCache)
        => _memoryCache = memoryCache;

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        _memoryCache.TryGetValue(key, out string? cachedJson);

        if (cachedJson is null)
            return Task.FromResult<T?>(null);

        var value = JsonSerializer.Deserialize<T>(cachedJson);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var json = JsonSerializer.Serialize(value);
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
        };

        _memoryCache.Set(key, json, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }
}

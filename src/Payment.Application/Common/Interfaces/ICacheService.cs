namespace Payment.Application.Common.Interfaces;

// Abstraction for in-memory caching operations (e.g. Redis, MemoryCache).
public interface ICacheService
{
    // Retrieves a cached value by key, or null if not found.
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    // Stores a value in cache with an optional expiration time.
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    // Removes a cached value by key.
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

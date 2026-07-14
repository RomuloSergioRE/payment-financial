using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Interfaces;

namespace Payment.Application.Common.Behaviours;

// Intercepts cacheable requests and returns cached responses when available, avoiding handler execution.
// On cache miss, executes the handler and stores the result with the configured expiration.
// Only activates when TRequest implements ICachableRequest.
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, ICachableRequest
    where TResponse : class
{
    private readonly ICacheService _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(
        ICacheService cache,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    // Checks cache first; on hit returns early, on miss executes handler and stores result.
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cachedValue = await _cache.GetAsync<TResponse>(request.CacheKey, cancellationToken);

        if (cachedValue is not null)
        {
            _logger.LogInformation("Cache hit for {CacheKey}", request.CacheKey);
            return cachedValue;
        }

        _logger.LogInformation("Cache miss for {CacheKey}", request.CacheKey);

        // Cache miss — execute the pipeline to get the response.
        var response = await next();

        // Store the response in cache for subsequent requests.
        await _cache.SetAsync(request.CacheKey, response, request.CacheExpiration, cancellationToken);

        return response;
    }
}

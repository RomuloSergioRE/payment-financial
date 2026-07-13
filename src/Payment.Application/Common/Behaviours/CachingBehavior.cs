using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Interfaces;

namespace Payment.Application.Common.Behaviours;

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

        var response = await next();

        await _cache.SetAsync(request.CacheKey, response, request.CacheExpiration, cancellationToken);

        return response;
    }
}

public interface ICachableRequest
{
    string CacheKey { get; }
    TimeSpan? CacheExpiration { get; }
}

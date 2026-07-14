using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Payment.Api.RateLimiting;

// Per-user rate limiter using a token bucket algorithm.
// Partitions by user ID (from JWT), falling back to IP address for unauthenticated callers.
// Allows 20 requests per minute with a burst queue of 5 additional requests.
public sealed class UserRateLimiter : IRateLimiterPolicy<string>
{
    private readonly ILogger<UserRateLimiter> _logger;

    public UserRateLimiter(ILogger<UserRateLimiter> logger)
    {
        _logger = logger;
        OnRejected = OnRejectedAsync;
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get; }

    // Returns a 429 response and logs which user hit the rate limit
    private ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken ct)
    {
        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var userId = context.HttpContext.Items["UserId"]?.ToString() ?? "unknown";
        _logger.LogWarning(
            "Rate limit exceeded for user {UserId}", userId);

        return ValueTask.CompletedTask;
    }

    // Determines the rate-limit partition key: prefer user ID, then IP, then anonymous
    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var userId = httpContext.Items["UserId"]?.ToString()
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetTokenBucketLimiter(userId,
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 20,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                TokensPerPeriod = 20,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5,
                AutoReplenishment = true
            });
    }
}

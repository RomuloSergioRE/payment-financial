using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Payment.Api.RateLimiting;

public sealed class UserRateLimiter : IRateLimiterPolicy<string>
{
    private readonly ILogger<UserRateLimiter> _logger;

    public UserRateLimiter(ILogger<UserRateLimiter> logger)
    {
        _logger = logger;
        OnRejected = OnRejectedAsync;
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get; }

    private ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken ct)
    {
        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var userId = context.HttpContext.Items["UserId"]?.ToString() ?? "unknown";
        _logger.LogWarning(
            "Rate limit exceeded for user {UserId}", userId);

        return ValueTask.CompletedTask;
    }

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

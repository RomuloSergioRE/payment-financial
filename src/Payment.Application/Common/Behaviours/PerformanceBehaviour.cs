using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Common.Behaviours;

// Monitors request execution time and logs a warning when it exceeds the threshold.
// Positioned near the outer pipeline so it measures the full round-trip including other behaviours.
public sealed class PerformanceBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehaviour<TRequest, TResponse>> _logger;
    private const int ThresholdMilliseconds = 500;

    public PerformanceBehaviour(ILogger<PerformanceBehaviour<TRequest, TResponse>> logger)
        => _logger = logger;

    // Starts a stopwatch, executes the pipeline, and warns if execution exceeded 500 ms.
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();

        var response = await next();

        timer.Stop();

        // Log a warning only when the request took longer than the configured threshold.
        if (timer.ElapsedMilliseconds > ThresholdMilliseconds)
        {
            _logger.LogWarning(
                "Long running request: {Name} ({ElapsedMilliseconds}ms)",
                typeof(TRequest).Name,
                timer.ElapsedMilliseconds);
        }

        return response;
    }
}

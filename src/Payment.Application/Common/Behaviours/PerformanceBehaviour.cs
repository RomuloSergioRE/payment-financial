using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Common.Behaviours;

public sealed class PerformanceBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehaviour<TRequest, TResponse>> _logger;
    private const int ThresholdMilliseconds = 500;

    public PerformanceBehaviour(ILogger<PerformanceBehaviour<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();

        var response = await next();

        timer.Stop();

        if (timer.ElapsedMilliseconds > ThresholdMilliseconds)
        {
            _logger.LogWarning(
                "Long running request: {Name} ({ElapsedMilliseconds}ms) {@Request}",
                typeof(TRequest).Name,
                timer.ElapsedMilliseconds,
                request);
        }

        return response;
    }
}

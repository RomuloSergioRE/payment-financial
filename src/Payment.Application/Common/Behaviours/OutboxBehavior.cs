using System.Text.Json;
using MediatR;
using Payment.Application.Common.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Application.Common.Behaviours;

// Persists outbox messages for publishable requests so domain events can be dispatched reliably.
// Runs after the handler succeeds — only requests implementing IPublishableRequest produce outbox entries.
public sealed class OutboxBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IPaymentDbContext _context;

    public OutboxBehavior(IPaymentDbContext context)
        => _context = context;

    // Executes the handler first, then writes an outbox message if the request is publishable.
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        // Only create an outbox entry for requests that implement IPublishableRequest.
        if (request is IPublishableRequest publishable)
        {
            var eventType = typeof(TRequest).Name;
            var payload = JsonSerializer.Serialize(new
            {
                EventType = eventType,
                Data = response,
                Timestamp = DateTime.UtcNow
            });

            var outboxMessage = new OutboxMessage(eventType, payload);
            _context.OutboxMessages.Add(outboxMessage);
        }

        return response;
    }
}

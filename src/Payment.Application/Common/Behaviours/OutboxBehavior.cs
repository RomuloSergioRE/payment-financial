using System.Text.Json;
using MediatR;
using Payment.Application.Common.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Application.Common.Behaviours;

public sealed class OutboxBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IPaymentDbContext _context;

    public OutboxBehavior(IPaymentDbContext context)
        => _context = context;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

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

public interface IPublishableRequest { }

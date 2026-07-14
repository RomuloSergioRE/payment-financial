using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Interfaces;
using Payment.Domain.Events;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Common.Behaviours;

// Collects domain events raised by Payment entities after the handler runs, then logs and clears them.
// Positioned after the handler so all entity mutations have already occurred before event dispatch.
public sealed class DomainEventDispatcherBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IPaymentDbContext _context;
    private readonly ILogger<DomainEventDispatcherBehavior<TRequest, TResponse>> _logger;

    public DomainEventDispatcherBehavior(
        IPaymentDbContext context,
        ILogger<DomainEventDispatcherBehavior<TRequest, TResponse>> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Executes the handler, then publishes any domain events raised by Payment entities.
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        // Query EF Core change tracker for Payment entities that have pending domain events.
        var entities = _context.ChangeTracker.Entries<PaymentEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        foreach (var entity in entities)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                _logger.LogInformation(
                    "Publishing domain event: {EventType} for {EntityId}",
                    domainEvent.GetType().Name, entity.Id);

                // Domain events can be published to message bus here
                // For now, just log them
            }

            entity.ClearDomainEvents();
        }

        return response;
    }
}

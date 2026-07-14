namespace Payment.Domain.Events;

// Contract for all domain events raised by aggregate roots in the payment domain.
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}

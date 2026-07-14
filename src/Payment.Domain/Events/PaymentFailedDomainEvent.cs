namespace Payment.Domain.Events;

// Event raised when a payment fails during processing.
// Includes the error message returned by the gateway, if any.
public sealed class PaymentFailedDomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid PaymentId { get; init; }
    public Guid UserId { get; init; }
    public string? ErrorMessage { get; init; }
}

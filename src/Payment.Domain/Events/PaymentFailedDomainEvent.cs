namespace Payment.Domain.Events;

public sealed class PaymentFailedDomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid PaymentId { get; init; }
    public Guid UserId { get; init; }
    public string? ErrorMessage { get; init; }
}

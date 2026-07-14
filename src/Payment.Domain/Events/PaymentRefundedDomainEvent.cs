namespace Payment.Domain.Events;

// Event raised when a previously completed payment has been refunded.
// Optionally includes a reason for the refund.
public sealed class PaymentRefundedDomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid PaymentId { get; init; }
    public Guid UserId { get; init; }
    public string? Reason { get; init; }
}

namespace Payment.Domain.Events;

// Event raised when a payment successfully transitions to Completed.
// Carries the payment and user identifiers along with transaction details.
public sealed class PaymentCompletedDomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid PaymentId { get; init; }
    public Guid UserId { get; init; }
    public string PlanType { get; init; } = null!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = null!;
    public string PaymentMethod { get; init; } = null!;
    public DateTime PaidAt { get; init; }
}

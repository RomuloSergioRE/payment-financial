namespace Payment.Application.Common.Models;

// Event payload published when a payment is successfully completed.
public sealed record PaymentCompletedEvent(
    string EventId,
    string Type,
    DateTime Timestamp,
    PaymentCompletedData Data);

// Inner data for the payment completed event.
public sealed record PaymentCompletedData(
    Guid PaymentId,
    Guid UserId,
    string PlanType,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    DateTime PaidAt);

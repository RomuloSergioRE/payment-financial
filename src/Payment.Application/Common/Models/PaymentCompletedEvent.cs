namespace Payment.Application.Common.Models;

public sealed record PaymentCompletedEvent(
    string EventId,
    string Type,
    DateTime Timestamp,
    PaymentCompletedData Data);

public sealed record PaymentCompletedData(
    Guid PaymentId,
    Guid UserId,
    string PlanType,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    DateTime PaidAt);

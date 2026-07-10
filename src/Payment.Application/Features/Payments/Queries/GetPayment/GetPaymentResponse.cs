namespace Payment.Application.Features.Payments.Queries.GetPayment;

public sealed record GetPaymentResponse(
    Guid PaymentId,
    Guid UserId,
    string PlanType,
    decimal Amount,
    string Currency,
    string Method,
    string Status,
    string? GatewayPaymentId,
    DateTime? PaidAt,
    DateTime CreatedAt);

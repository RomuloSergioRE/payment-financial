namespace Payment.Application.Features.Payments.Queries.GetPayment;

// Response DTO containing the full details of a single payment.
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

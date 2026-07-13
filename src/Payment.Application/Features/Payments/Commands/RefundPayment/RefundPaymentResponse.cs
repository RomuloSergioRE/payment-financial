namespace Payment.Application.Features.Payments.Commands.RefundPayment;

public sealed record RefundPaymentResponse(
    Guid PaymentId,
    string Status,
    string? ErrorMessage = null);

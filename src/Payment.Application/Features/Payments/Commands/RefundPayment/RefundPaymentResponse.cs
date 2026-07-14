namespace Payment.Application.Features.Payments.Commands.RefundPayment;

// Response returned after a refund payment attempt.
public sealed record RefundPaymentResponse(
    Guid PaymentId,
    string Status,
    string? ErrorMessage = null);

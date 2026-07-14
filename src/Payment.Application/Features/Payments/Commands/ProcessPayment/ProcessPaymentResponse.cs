namespace Payment.Application.Features.Payments.Commands.ProcessPayment;

// Response returned after a payment processing attempt.
public sealed record ProcessPaymentResponse(
    Guid PaymentId,
    string Status,
    string? ErrorMessage);

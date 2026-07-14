namespace Payment.Application.Features.Payments.Commands.CancelPayment;

// Response returned after a cancel payment attempt.
public sealed record CancelPaymentResponse(
    Guid PaymentId,
    string Status);

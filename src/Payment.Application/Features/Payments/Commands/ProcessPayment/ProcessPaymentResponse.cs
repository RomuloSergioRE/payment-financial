namespace Payment.Application.Features.Payments.Commands.ProcessPayment;

public sealed record ProcessPaymentResponse(
    Guid PaymentId,
    string Status,
    string? ErrorMessage);

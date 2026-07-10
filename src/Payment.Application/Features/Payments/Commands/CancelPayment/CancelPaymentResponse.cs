namespace Payment.Application.Features.Payments.Commands.CancelPayment;

public sealed record CancelPaymentResponse(
    Guid PaymentId,
    string Status);

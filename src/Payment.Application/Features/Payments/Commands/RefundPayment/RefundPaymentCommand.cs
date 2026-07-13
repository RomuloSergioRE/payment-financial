using MediatR;

namespace Payment.Application.Features.Payments.Commands.RefundPayment;

public sealed record RefundPaymentCommand(
    Guid PaymentId,
    Guid UserId,
    string? Reason = null) : IRequest<RefundPaymentResponse>;

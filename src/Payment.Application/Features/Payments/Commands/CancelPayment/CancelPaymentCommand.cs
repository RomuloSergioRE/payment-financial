using MediatR;

namespace Payment.Application.Features.Payments.Commands.CancelPayment;

public sealed record CancelPaymentCommand(
    Guid PaymentId,
    Guid UserId) : IRequest<CancelPaymentResponse>;

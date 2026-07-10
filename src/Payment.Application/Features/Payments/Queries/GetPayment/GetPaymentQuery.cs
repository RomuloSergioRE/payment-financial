using MediatR;

namespace Payment.Application.Features.Payments.Queries.GetPayment;

public sealed record GetPaymentQuery(
    Guid PaymentId,
    Guid UserId) : IRequest<GetPaymentResponse>;

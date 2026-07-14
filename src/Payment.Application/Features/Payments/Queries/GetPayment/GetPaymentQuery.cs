using MediatR;

namespace Payment.Application.Features.Payments.Queries.GetPayment;

// Query to retrieve a single payment by ID, scoped to the requesting user.
public sealed record GetPaymentQuery(
    Guid PaymentId,
    Guid UserId) : IRequest<GetPaymentResponse>;

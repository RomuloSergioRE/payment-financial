using MediatR;

namespace Payment.Application.Features.Payments.Queries.ListPayments;

public sealed record ListPaymentsQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 10,
    string? Status = null) : IRequest<ListPaymentsResponse>;

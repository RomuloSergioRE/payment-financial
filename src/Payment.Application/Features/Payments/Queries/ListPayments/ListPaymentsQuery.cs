using MediatR;

namespace Payment.Application.Features.Payments.Queries.ListPayments;

// Query to list payments for a user with pagination and optional status filter.
public sealed record ListPaymentsQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 10,
    string? Status = null) : IRequest<ListPaymentsResponse>;

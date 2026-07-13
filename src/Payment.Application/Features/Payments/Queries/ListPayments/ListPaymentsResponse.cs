using Payment.Application.Common.Models;

namespace Payment.Application.Features.Payments.Queries.ListPayments;

public sealed record ListPaymentsResponse(
    PagedResult<PaymentSummary> Payments);

public sealed record PaymentSummary(
    Guid PaymentId,
    string Status,
    decimal Amount,
    string Currency,
    string Method,
    string PlanType,
    DateTime CreatedAt,
    DateTime? PaidAt);

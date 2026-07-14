using Payment.Application.Common.Models;

namespace Payment.Application.Features.Payments.Queries.ListPayments;

// Response containing a paginated list of payment summaries.
public sealed record ListPaymentsResponse(
    PagedResult<PaymentSummary> Payments);

// Summarized payment data for list views.
public sealed record PaymentSummary(
    Guid PaymentId,
    string Status,
    decimal Amount,
    string Currency,
    string Method,
    string PlanType,
    DateTime CreatedAt,
    DateTime? PaidAt);

using FluentValidation;

namespace Payment.Application.Features.Payments.Queries.ListPayments;

public sealed class ListPaymentsQueryValidator : AbstractValidator<ListPaymentsQuery>
{
    public ListPaymentsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.Status)
            .Must(s => s == null || new[] { "pending", "processing", "completed", "failed", "refunded", "cancelled" }
                .Contains(s.ToLowerInvariant()))
            .WithMessage("Status must be one of: pending, processing, completed, failed, refunded, cancelled.");
    }
}

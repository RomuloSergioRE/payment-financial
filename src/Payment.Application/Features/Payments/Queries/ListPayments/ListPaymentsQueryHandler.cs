using MediatR;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Common.Interfaces;
using Payment.Application.Common.Models;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Payments.Queries.ListPayments;

public sealed class ListPaymentsQueryHandler
    : IRequestHandler<ListPaymentsQuery, ListPaymentsResponse>
{
    private readonly IPaymentDbContext _context;

    public ListPaymentsQueryHandler(IPaymentDbContext context)
        => _context = context;

    public async Task<ListPaymentsResponse> Handle(
        ListPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Payments
            .Where(p => p.UserId == request.UserId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<PaymentStatus>(request.Status, true, out var status))
                query = query.Where(p => p.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PaymentSummary(
                p.Id,
                p.Status.ToString().ToLowerInvariant(),
                p.Amount.Amount,
                p.Amount.Currency,
                p.Method.ToString().ToLowerInvariant(),
                p.PlanType.ToString().ToLowerInvariant(),
                p.CreatedAt,
                p.PaidAt))
            .ToListAsync(cancellationToken);

        var pagedResult = PagedResult<PaymentSummary>.Create(
            payments, request.Page, request.PageSize, totalCount);

        return new ListPaymentsResponse(pagedResult);
    }
}

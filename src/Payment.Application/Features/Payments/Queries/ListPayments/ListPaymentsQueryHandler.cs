using MediatR;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Common.Interfaces;
using Payment.Application.Common.Models;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Payments.Queries.ListPayments;

// Handles ListPaymentsQuery by querying payments with optional status filter and pagination.
public sealed class ListPaymentsQueryHandler
    : IRequestHandler<ListPaymentsQuery, ListPaymentsResponse>
{
    private readonly IPaymentDbContext _context;

    public ListPaymentsQueryHandler(IPaymentDbContext context)
        => _context = context;

    // Returns a paginated list of payments for the specified user.
    public async Task<ListPaymentsResponse> Handle(
        ListPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        // PASSO 1: Construir query base filtrando por UserId (asNoTracking para performance).
        var query = _context.Payments
            .Where(p => p.UserId == request.UserId)
            .AsNoTracking();

        // PASSO 2: Aplicar filtro opcional de status, se informado.
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<PaymentStatus>(request.Status, true, out var status))
                query = query.Where(p => p.Status == status);
        }

        // PASSO 3: Contar total de registros para paginação.
        var totalCount = await query.CountAsync(cancellationToken);

        // PASSO 4: Buscar página ordenada por data de criação (mais recente primeiro).
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

        // PASSO 5: Montar resultado paginado.
        var pagedResult = PagedResult<PaymentSummary>.Create(
            payments, request.Page, request.PageSize, totalCount);

        return new ListPaymentsResponse(pagedResult);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Exceptions;
using Payment.Application.Common.Interfaces;

namespace Payment.Application.Features.Payments.Queries.GetPayment;

public sealed class GetPaymentQueryHandler
    : IRequestHandler<GetPaymentQuery, GetPaymentResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly ILogger<GetPaymentQueryHandler> _logger;

    public GetPaymentQueryHandler(
        IPaymentDbContext context,
        ILogger<GetPaymentQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetPaymentResponse> Handle(
        GetPaymentQuery request,
        CancellationToken cancellationToken)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

        if (payment is null)
        {
            _logger.LogWarning("Payment {PaymentId} not found", request.PaymentId);
            throw new NotFoundException("Payment", request.PaymentId);
        }

        if (payment.UserId != request.UserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access payment {PaymentId} belonging to {OwnerId}",
                request.UserId, request.PaymentId, payment.UserId);
            throw new NotFoundException("Payment", request.PaymentId);
        }

        return new GetPaymentResponse(
            payment.Id,
            payment.UserId,
            payment.PlanType.ToString(),
            payment.Amount.Amount,
            payment.Amount.Currency,
            payment.Method.ToString(),
            payment.Status.ToString(),
            payment.GatewayPaymentId,
            payment.PaidAt,
            payment.CreatedAt);
    }
}

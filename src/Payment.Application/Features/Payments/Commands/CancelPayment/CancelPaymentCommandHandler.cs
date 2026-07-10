using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Exceptions;
using Payment.Application.Common.Interfaces;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Payments.Commands.CancelPayment;

public sealed class CancelPaymentCommandHandler
    : IRequestHandler<CancelPaymentCommand, CancelPaymentResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly ILogger<CancelPaymentCommandHandler> _logger;

    public CancelPaymentCommandHandler(
        IPaymentDbContext context,
        ILogger<CancelPaymentCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CancelPaymentResponse> Handle(
        CancelPaymentCommand request,
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
                "User {UserId} attempted to cancel payment {PaymentId} belonging to {OwnerId}",
                request.UserId, request.PaymentId, payment.UserId);
            throw new NotFoundException("Payment", request.PaymentId);
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            _logger.LogWarning(
                "Cannot cancel payment {PaymentId} with status {Status}",
                request.PaymentId, payment.Status);
            throw new Domain.Exceptions.PaymentException(
                $"Cannot cancel payment with status '{payment.Status}'. Only pending payments can be cancelled.");
        }

        payment.MarkRefunded();
        _context.PaymentLogs.Add(new Domain.Entities.PaymentLog(
            payment.Id, "payment.cancelled"));

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payment {PaymentId} cancelled by user {UserId}",
            request.PaymentId, request.UserId);

        return new CancelPaymentResponse(
            payment.Id,
            payment.Status.ToString().ToLowerInvariant());
    }
}

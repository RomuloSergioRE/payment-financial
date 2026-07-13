using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Exceptions;
using Payment.Application.Common.Interfaces;
using Payment.Application.Common.Models;
using Payment.Domain.Entities;
using Payment.Domain.Exceptions;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Features.Payments.Commands.RefundPayment;

public sealed class RefundPaymentCommandHandler
    : IRequestHandler<RefundPaymentCommand, RefundPaymentResponse>
{
    private readonly IPaymentDbContext _context;
    private readonly IPaymentGateway _gateway;
    private readonly ILogger<RefundPaymentCommandHandler> _logger;

    public RefundPaymentCommandHandler(
        IPaymentDbContext context,
        IPaymentGateway gateway,
        ILogger<RefundPaymentCommandHandler> logger)
    {
        _context = context;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<RefundPaymentResponse> Handle(
        RefundPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == command.PaymentId, cancellationToken);

        if (payment is null)
            throw new NotFoundException(nameof(PaymentEntity), command.PaymentId);

        if (payment.UserId != command.UserId)
            throw new PaymentException("You do not have access to refund this payment.");

        payment.MarkRefunded();

        _context.PaymentLogs.Add(new PaymentLog(
            payment.Id, PaymentLog.EventTypes.Refunded,
            new { reason = command.Reason }));

        try
        {
            var result = await _gateway.RefundAsync(
                payment.Amount.Amount, payment.GatewayPaymentId!);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Gateway refund failed for payment {PaymentId}: {Message}",
                    payment.Id, result.GatewayMessage);

                return new RefundPaymentResponse(
                    payment.Id, "failed", result.GatewayMessage);
            }

            _logger.LogInformation(
                "Refund processed for payment {PaymentId}: {GatewayPaymentId}",
                payment.Id, payment.GatewayPaymentId);

            return new RefundPaymentResponse(payment.Id, "refunded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refund failed for payment {PaymentId}", payment.Id);
            return new RefundPaymentResponse(payment.Id, "failed", ex.Message);
        }
    }
}

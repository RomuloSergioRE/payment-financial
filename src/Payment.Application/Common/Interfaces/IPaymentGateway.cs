using Payment.Application.Common.Models;

namespace Payment.Application.Common.Interfaces;

public interface IPaymentGateway
{
    Task<PaymentResult> ProcessCreditCardAsync(
        decimal amount, string cardNumber, string cvv,
        int expiryMonth, int expiryYear, string holderName);

    Task<PaymentResult> ProcessPixAsync(decimal amount);

    Task<PaymentResult> ProcessBoletoAsync(decimal amount);

    Task<PaymentResult> RefundAsync(decimal amount, string gatewayPaymentId);
}

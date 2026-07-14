using Payment.Application.Common.Models;

namespace Payment.Application.Common.Interfaces;

// Abstraction for the external payment gateway, encapsulating
// all payment processing and refund operations.
public interface IPaymentGateway
{
    // Processes a credit card payment with the given card details.
    Task<PaymentResult> ProcessCreditCardAsync(
        decimal amount, string cardNumber, string cvv,
        int expiryMonth, int expiryYear, string holderName);

    // Processes a Pix instant payment for the specified amount.
    Task<PaymentResult> ProcessPixAsync(decimal amount);

    // Processes a Boleto bank slip payment for the specified amount.
    Task<PaymentResult> ProcessBoletoAsync(decimal amount);

    // Refunds the specified amount for a previously processed payment.
    Task<PaymentResult> RefundAsync(decimal amount, string gatewayPaymentId);
}

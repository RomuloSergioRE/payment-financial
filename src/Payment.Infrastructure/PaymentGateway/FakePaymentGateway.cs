using System.Text.Json;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Interfaces;
using Payment.Application.Common.Models;

namespace Payment.Infrastructure.PaymentGateway;

// Simulated payment gateway for development/testing purposes.
// Mimics real gateway behavior with random delays, card validation, and configurable success rate.
public sealed class FakePaymentGateway : IPaymentGateway
{
    private readonly ILogger<FakePaymentGateway> _logger;

    // 90% of simulated transactions will succeed
    private const double SuccessRate = 0.9;

    public FakePaymentGateway(ILogger<FakePaymentGateway> logger)
        => _logger = logger;

    // Simulates credit card processing with Luhn validation, expiry check, and random approval/decline
    public async Task<PaymentResult> ProcessCreditCardAsync(
        decimal amount, string cardNumber, string cvv,
        int expiryMonth, int expiryYear, string holderName)
    {
        if (!LuhnCheck(cardNumber))
            return new PaymentResult(
                false, string.Empty, "Invalid card number", null);

        if (expiryYear < DateTime.UtcNow.Year ||
            (expiryYear == DateTime.UtcNow.Year && expiryMonth < DateTime.UtcNow.Month))
            return new PaymentResult(
                false, string.Empty, "Card expired", null);

        // Simulate network latency
        await Task.Delay(Random.Shared.Next(500, 2000));

        var success = Random.Shared.NextDouble() < SuccessRate;

        if (success)
        {
            var gatewayId = $"cc_{Guid.NewGuid():N}";
            var brand = GetFakeBrand(cardNumber);
            _logger.LogInformation("Credit card payment approved: {Id}", gatewayId);
            var raw = JsonSerializer.Serialize(new
            {
                id = gatewayId,
                status = "approved",
                brand
            });
            return new PaymentResult(true, gatewayId, "Approved", raw);
        }

        return new PaymentResult(false, string.Empty, "Card declined by issuer", null);
    }

    // Simulates instant PIX payment (always succeeds in the fake implementation)
    public async Task<PaymentResult> ProcessPixAsync(decimal amount)
    {
        await Task.Delay(Random.Shared.Next(100, 500));

        var pixCode = Guid.NewGuid().ToString("N").ToUpper()[..32];
        var gatewayId = $"pix_{Guid.NewGuid():N}";

        _logger.LogInformation("PIX payment completed: {Id}", gatewayId);

        var raw = JsonSerializer.Serialize(new
        {
            id = gatewayId,
            pix_code = pixCode,
            status = "completed"
        });

        return new PaymentResult(true, gatewayId, "PIX completed", raw);
    }

    // Simulates boleto generation (always returns pending since boletos require waiting for payment)
    public Task<PaymentResult> ProcessBoletoAsync(decimal amount)
    {
        var nossoNumero = new Bogus.Faker().Random.String2(47, "0123456789");
        var gatewayId = $"boleto_{Guid.NewGuid():N}";
        var expiresAt = DateTime.UtcNow.AddDays(3);

        _logger.LogInformation("Boleto generated: {Id}, expires: {Expires}", gatewayId, expiresAt);

        var raw = JsonSerializer.Serialize(new
        {
            id = gatewayId,
            nosso_numero = nossoNumero,
            expires_at = expiresAt.ToString("O"),
            status = "pending"
        });

        return Task.FromResult(new PaymentResult(
            true, gatewayId, "Boleto generated", raw));
    }

    // Simulates refund processing with random delay
    public async Task<PaymentResult> RefundAsync(decimal amount, string gatewayPaymentId)
    {
        await Task.Delay(Random.Shared.Next(200, 800));

        var refundId = $"refund_{Guid.NewGuid():N}";
        _logger.LogInformation("Refund processed: {RefundId} for gateway payment: {GatewayPaymentId}",
            refundId, gatewayPaymentId);

        var raw = JsonSerializer.Serialize(new
        {
            id = refundId,
            original_payment = gatewayPaymentId,
            status = "refunded",
            amount
        });

        return new PaymentResult(true, refundId, "Refund processed", raw);
    }

    // Luhn algorithm for credit card number validation
    // Doubles every second digit from right to left, sums digits, checks divisibility by 10
    private static bool LuhnCheck(string cardNumber)
    {
        var digits = cardNumber.Where(char.IsDigit).Select(c => int.Parse(c.ToString())).ToArray();
        var sum = 0;
        var alt = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var digit = digits[i];
            if (alt)
            {
                // Double the digit; if result exceeds 9, subtract 9 (equivalent to summing its digits)
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            alt = !alt;
        }
        return sum % 10 == 0;
    }

    // Determines card brand from the first digit (IIN/BIN prefix)
    private static string GetFakeBrand(string cardNumber)
        => cardNumber[0] switch
        {
            '4' => "Visa",
            '5' or '2' => "Mastercard",
            '3' => "Amex",
            '6' => "Discover",
            _ => "Unknown"
        };
}

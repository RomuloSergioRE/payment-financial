using System.Text.Json;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Interfaces;
using Payment.Application.Common.Models;

namespace Payment.Infrastructure.PaymentGateway;

public sealed class FakePaymentGateway : IPaymentGateway
{
    private readonly Random _random = new();
    private readonly ILogger<FakePaymentGateway> _logger;

    private const double SuccessRate = 0.9;

    public FakePaymentGateway(ILogger<FakePaymentGateway> logger)
        => _logger = logger;

    public Task<PaymentResult> ProcessCreditCardAsync(
        decimal amount, string cardNumber, string cvv,
        int expiryMonth, int expiryYear, string holderName)
    {
        if (!LuhnCheck(cardNumber))
            return Task.FromResult(new PaymentResult(
                false, string.Empty, "Invalid card number", null));

        if (expiryYear < DateTime.UtcNow.Year ||
            (expiryYear == DateTime.UtcNow.Year && expiryMonth < DateTime.UtcNow.Month))
            return Task.FromResult(new PaymentResult(
                false, string.Empty, "Card expired", null));

        Thread.Sleep(_random.Next(500, 2000));

        var success = _random.NextDouble() < SuccessRate;

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
            return Task.FromResult(new PaymentResult(
                true, gatewayId, "Approved", raw));
        }

        return Task.FromResult(new PaymentResult(
            false, string.Empty, "Card declined by issuer", null));
    }

    public Task<PaymentResult> ProcessPixAsync(decimal amount)
    {
        Thread.Sleep(_random.Next(100, 500));

        var pixCode = Guid.NewGuid().ToString("N").ToUpper()[..32];
        var gatewayId = $"pix_{Guid.NewGuid():N}";

        _logger.LogInformation("PIX payment completed: {Id}", gatewayId);

        var raw = JsonSerializer.Serialize(new
        {
            id = gatewayId,
            pix_code = pixCode,
            status = "completed"
        });

        return Task.FromResult(new PaymentResult(
            true, gatewayId, "PIX completed", raw));
    }

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
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            alt = !alt;
        }
        return sum % 10 == 0;
    }

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

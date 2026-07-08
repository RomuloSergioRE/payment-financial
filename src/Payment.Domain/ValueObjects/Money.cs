using Payment.Domain.Exceptions;

namespace Payment.Domain.ValueObjects;

public sealed class Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount <= 0)
            throw new PaymentException("Amount must be positive");

        if (string.IsNullOrWhiteSpace(currency))
            throw new PaymentException("Currency is required");

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public override string ToString() => $"{Currency} {Amount:F2}";
}

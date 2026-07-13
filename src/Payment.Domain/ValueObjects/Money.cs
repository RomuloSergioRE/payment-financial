using Payment.Domain.Exceptions;

namespace Payment.Domain.ValueObjects;

public sealed class Money : IEquatable<Money>
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

    public bool Equals(Money? other)
    {
        if (other is null) return false;
        return Amount == other.Amount && Currency == other.Currency;
    }

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString() => $"{Currency} {Amount:F2}";

    public static bool operator ==(Money? left, Money? right) => Equals(left, right);
    public static bool operator !=(Money? left, Money? right) => !Equals(left, right);
}

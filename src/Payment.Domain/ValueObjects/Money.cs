using Payment.Domain.Exceptions;

namespace Payment.Domain.ValueObjects;

// Immutable value object representing a monetary amount with its currency code.
// Validates that the amount is positive and the currency is non-empty.
public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    // Create a new Money instance. Throws PaymentException if amount <= 0 or currency is empty.
    public Money(decimal amount, string currency)
    {
        if (amount <= 0)
            throw new PaymentException("Amount must be positive");

        if (string.IsNullOrWhiteSpace(currency))
            throw new PaymentException("Currency is required");

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    // Value-based equality: two Money instances are equal when Amount and Currency match.
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

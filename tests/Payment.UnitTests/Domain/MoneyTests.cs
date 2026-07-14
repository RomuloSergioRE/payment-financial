using FluentAssertions;
using Payment.Domain.Exceptions;
using Payment.Domain.ValueObjects;

namespace Payment.UnitTests.Domain;

// Tests for the Money value object: creation, currency normalization, and validation rules.
public class MoneyTests
{
    // Given a valid positive amount and currency, When creating Money, Then values are stored correctly.
    [Fact]
    public void ValidAmountAndCurrency_CreatesSuccessfully()
    {
        // Arrange
        var money = new Money(29.90m, "BRL");

        // Assert
        money.Amount.Should().Be(29.90m);
        money.Currency.Should().Be("BRL");
    }

    // Given a lowercase currency string, When creating Money, Then currency is normalized to uppercase.
    [Fact]
    public void CurrencyIsUppercased()
    {
        // Arrange
        var money = new Money(10m, "brl");

        // Assert
        money.Currency.Should().Be("BRL");
    }

    // Given a zero amount, When creating Money, Then a PaymentException is thrown.
    [Fact]
    public void ZeroAmount_ThrowsPaymentException()
    {
        // Act
        var act = () => new Money(0m, "BRL");

        // Assert
        act.Should().Throw<PaymentException>()
            .WithMessage("*Amount must be positive*");
    }

    // Given a negative amount, When creating Money, Then a PaymentException is thrown.
    [Fact]
    public void NegativeAmount_ThrowsPaymentException()
    {
        // Act
        var act = () => new Money(-5m, "BRL");

        // Assert
        act.Should().Throw<PaymentException>()
            .WithMessage("*Amount must be positive*");
    }

    // Given an empty currency string, When creating Money, Then a PaymentException is thrown.
    [Fact]
    public void EmptyCurrency_ThrowsPaymentException()
    {
        // Act
        var act = () => new Money(10m, "");

        // Assert
        act.Should().Throw<PaymentException>()
            .WithMessage("*Currency is required*");
    }

    // Given a null currency, When creating Money, Then a PaymentException is thrown.
    [Fact]
    public void NullCurrency_ThrowsPaymentException()
    {
        // Act
        var act = () => new Money(10m, null!);

        // Assert
        act.Should().Throw<PaymentException>()
            .WithMessage("*Currency is required*");
    }

    // Given a valid Money instance, When calling ToString, Then formatted output is returned.
    [Fact]
    public void ToString_FormatsCorrectly()
    {
        // Arrange
        var money = new Money(29.90m, "BRL");

        // Assert
        money.ToString().Should().Be($"BRL {29.90m:F2}");
    }
}

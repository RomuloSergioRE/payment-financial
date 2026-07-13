using FluentAssertions;
using Payment.Domain.Exceptions;
using Payment.Domain.ValueObjects;

namespace Payment.UnitTests.Domain;

public class MoneyTests
{
    [Fact]
    public void ValidAmountAndCurrency_CreatesSuccessfully()
    {
        var money = new Money(29.90m, "BRL");

        money.Amount.Should().Be(29.90m);
        money.Currency.Should().Be("BRL");
    }

    [Fact]
    public void CurrencyIsUppercased()
    {
        var money = new Money(10m, "brl");

        money.Currency.Should().Be("BRL");
    }

    [Fact]
    public void ZeroAmount_ThrowsPaymentException()
    {
        var act = () => new Money(0m, "BRL");

        act.Should().Throw<PaymentException>()
            .WithMessage("*Amount must be positive*");
    }

    [Fact]
    public void NegativeAmount_ThrowsPaymentException()
    {
        var act = () => new Money(-5m, "BRL");

        act.Should().Throw<PaymentException>()
            .WithMessage("*Amount must be positive*");
    }

    [Fact]
    public void EmptyCurrency_ThrowsPaymentException()
    {
        var act = () => new Money(10m, "");

        act.Should().Throw<PaymentException>()
            .WithMessage("*Currency is required*");
    }

    [Fact]
    public void NullCurrency_ThrowsPaymentException()
    {
        var act = () => new Money(10m, null!);

        act.Should().Throw<PaymentException>()
            .WithMessage("*Currency is required*");
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var money = new Money(29.90m, "BRL");

        money.ToString().Should().Be($"BRL {29.90m:F2}");
    }
}

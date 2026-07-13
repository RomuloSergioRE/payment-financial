using FluentAssertions;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Exceptions;
using Payment.Domain.ValueObjects;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.UnitTests.Domain;

public class PaymentTests
{
    private static PaymentEntity CreateValidPayment(
        Guid? userId = null,
        PlanType planType = PlanType.Pro,
        decimal amount = 29.90m,
        PaymentMethod method = PaymentMethod.CreditCard)
    {
        return new PaymentEntity(
            userId ?? Guid.NewGuid(),
            planType,
            new Money(amount, "BRL"),
            method,
            Guid.NewGuid().ToString());
    }

    [Fact]
    public void Constructor_SetsDefaultsCorrectly()
    {
        var userId = Guid.NewGuid();
        var payment = CreateValidPayment(userId: userId);

        payment.Id.Should().NotBeEmpty();
        payment.UserId.Should().Be(userId);
        payment.PlanType.Should().Be(PlanType.Pro);
        payment.Amount.Amount.Should().Be(29.90m);
        payment.Amount.Currency.Should().Be("BRL");
        payment.Method.Should().Be(PaymentMethod.CreditCard);
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.GatewayPaymentId.Should().BeNull();
        payment.GatewayResponse.Should().BeNull();
        payment.PaidAt.Should().BeNull();
        payment.RefundedAt.Should().BeNull();
        payment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkProcessing_SetsStatusToProcessing()
    {
        var payment = CreateValidPayment();

        payment.MarkProcessing();

        payment.Status.Should().Be(PaymentStatus.Processing);
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkCompleted_SetsStatusAndGatewayFields()
    {
        var payment = CreateValidPayment();
        var gatewayId = "cc_abc123";
        var response = "{\"status\":\"approved\"}";

        payment.MarkCompleted(gatewayId, response);

        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.GatewayPaymentId.Should().Be(gatewayId);
        payment.GatewayResponse.Should().Be(response);
        payment.PaidAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkFailed_SetsStatusToFailed()
    {
        var payment = CreateValidPayment();

        payment.MarkFailed();

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkRefunded_SetsStatusToRefunded()
    {
        var payment = CreateValidPayment();

        payment.MarkRefunded();

        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkCancelled_SetsStatusToCancelled()
    {
        var payment = CreateValidPayment();

        payment.MarkCancelled();

        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_WithPixMethod_SetsCorrectly()
    {
        var payment = CreateValidPayment(method: PaymentMethod.Pix);

        payment.Method.Should().Be(PaymentMethod.Pix);
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Constructor_WithBoletoMethod_SetsCorrectly()
    {
        var payment = CreateValidPayment(method: PaymentMethod.Boleto);

        payment.Method.Should().Be(PaymentMethod.Boleto);
        payment.Status.Should().Be(PaymentStatus.Pending);
    }
}

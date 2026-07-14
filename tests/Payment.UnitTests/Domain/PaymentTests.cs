using FluentAssertions;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Exceptions;
using Payment.Domain.ValueObjects;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.UnitTests.Domain;

// Tests for the Payment entity: construction, state transitions, and business rule enforcement.
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

    // Given a valid userId, When creating a new Payment, Then all defaults are set correctly.
    [Fact]
    public void Constructor_SetsDefaultsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var payment = CreateValidPayment(userId: userId);

        // Assert
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

    // Given a payment in Pending status, When MarkProcessing is called, Then status transitions to Processing.
    [Fact]
    public void MarkProcessing_FromPending_SetsStatusToProcessing()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        payment.MarkProcessing();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Processing);
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // Given a payment already in Processing status, When MarkProcessing is called again, Then a PaymentException is thrown.
    [Fact]
    public void MarkProcessing_FromProcessing_ThrowsPaymentException()
    {
        // Arrange
        var payment = CreateValidPayment();
        payment.MarkProcessing();

        // Act
        var act = () => payment.MarkProcessing();

        // Assert
        act.Should().Throw<PaymentException>()
            .WithMessage("*Cannot transition to Processing from 'Processing'*");
    }

    // Given a payment in Processing status, When MarkCompleted is called, Then status, gateway fields, and PaidAt are set.
    [Fact]
    public void MarkCompleted_FromProcessing_SetsStatusAndGatewayFields()
    {
        // Arrange
        var payment = CreateValidPayment();
        var gatewayId = "cc_abc123";
        var response = "{\"status\":\"approved\"}";

        // Act
        payment.MarkProcessing();
        payment.MarkCompleted(gatewayId, response);

        // Assert
        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.GatewayPaymentId.Should().Be(gatewayId);
        payment.GatewayResponse.Should().Be(response);
        payment.PaidAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // Given a payment in Pending status, When MarkCompleted is called, Then a PaymentException is thrown.
    [Fact]
    public void MarkCompleted_FromPending_ThrowsPaymentException()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        var act = () => payment.MarkCompleted("gw", "{}");

        // Assert
        act.Should().Throw<PaymentException>()
            .WithMessage("*Cannot transition to Completed from 'Pending'*");
    }

    // Given a payment in Processing status, When MarkFailed is called, Then status transitions to Failed.
    [Fact]
    public void MarkFailed_FromProcessing_SetsStatusToFailed()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        payment.MarkProcessing();
        payment.MarkFailed();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // Given a payment in Pending status, When MarkFailed is called, Then a PaymentException is thrown.
    [Fact]
    public void MarkFailed_FromPending_ThrowsPaymentException()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        var act = () => payment.MarkFailed();

        // Assert
        act.Should().Throw<PaymentException>()
            .WithMessage("*Cannot transition to Failed from 'Pending'*");
    }

    // Given a payment in Completed status, When MarkRefunded is called, Then status transitions to Refunded and RefundedAt is set.
    [Fact]
    public void MarkRefunded_FromCompleted_SetsStatusToRefunded()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        payment.MarkProcessing();
        payment.MarkCompleted("gw_123", "{}");
        payment.MarkRefunded();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // Given a payment in Pending status, When MarkRefunded is called, Then a PaymentException is thrown.
    [Fact]
    public void MarkRefunded_FromPending_ThrowsPaymentException()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        var act = () => payment.MarkRefunded();

        // Assert
        act.Should().Throw<PaymentException>()
            .WithMessage("*Cannot transition to Refunded from 'Pending'*");
    }

    // Given a payment in Pending status, When MarkCancelled is called, Then status transitions to Cancelled.
    [Fact]
    public void MarkCancelled_FromPending_SetsStatusToCancelled()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        payment.MarkCancelled();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // Given a payment in Processing status, When MarkCancelled is called, Then a PaymentException is thrown.
    [Fact]
    public void MarkCancelled_FromProcessing_ThrowsPaymentException()
    {
        // Arrange
        var payment = CreateValidPayment();
        payment.MarkProcessing();

        // Act
        var act = () => payment.MarkCancelled();

        // Assert
        act.Should().Throw<PaymentException>()
            .WithMessage("*Cannot transition to Cancelled from 'Processing'*");
    }

    // Given a valid Pix payment, When created, Then the method is set to Pix and status is Pending.
    [Fact]
    public void Constructor_WithPixMethod_SetsCorrectly()
    {
        // Arrange
        var payment = CreateValidPayment(method: PaymentMethod.Pix);

        // Assert
        payment.Method.Should().Be(PaymentMethod.Pix);
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    // Given a valid Boleto payment, When created, Then the method is set to Boleto and status is Pending.
    [Fact]
    public void Constructor_WithBoletoMethod_SetsCorrectly()
    {
        // Arrange
        var payment = CreateValidPayment(method: PaymentMethod.Boleto);

        // Assert
        payment.Method.Should().Be(PaymentMethod.Boleto);
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    // Given a new payment, When transitioning through Pending -> Processing -> Completed, Then all intermediate statuses are valid and PaidAt is set.
    [Fact]
    public void FullLifecycle_PendingToCompleted()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        payment.MarkProcessing();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Processing);

        // Act
        payment.MarkCompleted("gw_full", "{}");

        // Assert
        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.PaidAt.Should().NotBeNull();
    }

    // Given a new payment, When transitioning from Pending to Cancelled, Then status is set to Cancelled.
    [Fact]
    public void FullLifecycle_PendingToCancelled()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        payment.MarkCancelled();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Cancelled);
    }
}

using Payment.Domain.Enums;
using Payment.Domain.Events;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.Entities;

// Represents a financial transaction within the system, enforcing valid state transitions
// and raising domain events for each lifecycle change.
public sealed class Payment
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public PlanType PlanType { get; private set; }
    public Money Amount { get; private set; } = null!;
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? GatewayPaymentId { get; private set; }
    public string? GatewayResponse { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Payment() { }

    // Enqueue a domain event to be dispatched after the transaction is committed.
    public void AddDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    // Remove all pending domain events. Called after they have been dispatched.
    public void ClearDomainEvents()
        => _domainEvents.Clear();

    // Factory method for unit tests only. Creates a fully formed Payment with default values.
    public static Payment CreateForTest(
        Guid? id = null,
        Guid? userId = null,
        PlanType planType = PlanType.Pro,
        PaymentStatus status = PaymentStatus.Pending,
        string? idempotencyKey = null)
    {
        var payment = new Payment
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            PlanType = planType,
            Amount = new Money(29.90m, "BRL"),
            Method = PaymentMethod.CreditCard,
            Status = status,
            IdempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        return payment;
    }

    // Initialize a new payment in Pending status with the given parameters.
    public Payment(Guid userId, PlanType planType, Money amount,
                   PaymentMethod method, string idempotencyKey)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        PlanType = planType;
        Amount = amount;
        Method = method;
        Status = PaymentStatus.Pending;
        IdempotencyKey = idempotencyKey;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Transition from Pending to Processing when the gateway starts processing.
    // Throws PaymentException if current status is not Pending.
    public void MarkProcessing()
    {
        if (Status != PaymentStatus.Pending)
            throw new Exceptions.PaymentException(
                $"Cannot transition to Processing from '{Status}'.");
        Status = PaymentStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    // Transition from Processing to Completed after successful gateway confirmation.
    // Throws PaymentException if current status is not Processing.
    public void MarkCompleted(string gatewayPaymentId, string gatewayResponse)
    {
        if (Status != PaymentStatus.Processing)
            throw new Exceptions.PaymentException(
                $"Cannot transition to Completed from '{Status}'.");
        Status = PaymentStatus.Completed;
        GatewayPaymentId = gatewayPaymentId;
        GatewayResponse = gatewayResponse;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Transition from Processing to Failed when the gateway rejects the payment.
    // Throws PaymentException if current status is not Processing.
    public void MarkFailed(string? errorMessage = null)
    {
        if (Status != PaymentStatus.Processing)
            throw new Exceptions.PaymentException(
                $"Cannot transition to Failed from '{Status}'.");
        Status = PaymentStatus.Failed;
        GatewayResponse = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }

    // Transition from Completed to Refunded after the payment has been reimbursed.
    // Throws PaymentException if current status is not Completed.
    public void MarkRefunded()
    {
        if (Status != PaymentStatus.Completed)
            throw new Exceptions.PaymentException(
                $"Cannot transition to Refunded from '{Status}'.");
        Status = PaymentStatus.Refunded;
        RefundedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Transition from Pending to Cancelled when the user or system cancels the payment.
    // Throws PaymentException if current status is not Pending.
    public void MarkCancelled()
    {
        if (Status != PaymentStatus.Pending)
            throw new Exceptions.PaymentException(
                $"Cannot transition to Cancelled from '{Status}'.");
        Status = PaymentStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}

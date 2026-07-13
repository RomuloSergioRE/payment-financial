using Payment.Domain.Enums;
using Payment.Domain.Events;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.Entities;

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

    public void AddDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents()
        => _domainEvents.Clear();

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

    public void MarkProcessing()
    {
        if (Status != PaymentStatus.Pending)
            throw new Exceptions.PaymentException(
                $"Cannot transition to Processing from '{Status}'.");
        Status = PaymentStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

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

    public void MarkFailed(string? errorMessage = null)
    {
        if (Status != PaymentStatus.Processing)
            throw new Exceptions.PaymentException(
                $"Cannot transition to Failed from '{Status}'.");
        Status = PaymentStatus.Failed;
        GatewayResponse = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkRefunded()
    {
        if (Status != PaymentStatus.Completed)
            throw new Exceptions.PaymentException(
                $"Cannot transition to Refunded from '{Status}'.");
        Status = PaymentStatus.Refunded;
        RefundedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCancelled()
    {
        if (Status != PaymentStatus.Pending)
            throw new Exceptions.PaymentException(
                $"Cannot transition to Cancelled from '{Status}'.");
        Status = PaymentStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}

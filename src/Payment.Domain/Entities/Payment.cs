using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.Entities;

public sealed class Payment
{
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

    private Payment() { }

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
        Status = PaymentStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompleted(string gatewayPaymentId, string gatewayResponse)
    {
        Status = PaymentStatus.Completed;
        GatewayPaymentId = gatewayPaymentId;
        GatewayResponse = gatewayResponse;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = PaymentStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkRefunded()
    {
        Status = PaymentStatus.Refunded;
        RefundedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

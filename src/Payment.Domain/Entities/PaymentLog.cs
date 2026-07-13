using System.Text.Json;

namespace Payment.Domain.Entities;

public sealed class PaymentLog
{
    public static class EventTypes
    {
        public const string Created = "payment.created";
        public const string Processing = "payment.processing";
        public const string Completed = "payment.completed";
        public const string Failed = "payment.failed";
        public const string Cancelled = "payment.cancelled";
    }

    public Guid Id { get; private set; }
    public Guid PaymentId { get; private set; }
    public string EventType { get; private set; } = null!;
    public string? Metadata { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PaymentLog() { }

    public PaymentLog(Guid paymentId, string eventType, object? metadata = null)
    {
        Id = Guid.NewGuid();
        PaymentId = paymentId;
        EventType = eventType;
        Metadata = metadata is not null ? JsonSerializer.Serialize(metadata) : null;
        CreatedAt = DateTime.UtcNow;
    }
}

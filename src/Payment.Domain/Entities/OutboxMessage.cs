namespace Payment.Domain.Entities;

// Represents a pending integration message that will be published asynchronously,
// ensuring at-least-once delivery via the outbox pattern.
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { }

    // Initialize a new outbox message with the given event type and serialized payload.
    public OutboxMessage(string eventType, string payload)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        Payload = payload;
        CreatedAt = DateTime.UtcNow;
    }

    // Mark the message as successfully processed by setting ProcessedAt to now.
    public void MarkProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }

    // Record the error that caused the processing to fail, for later inspection/retry.
    public void MarkFailed(string error)
    {
        Error = error;
    }
}

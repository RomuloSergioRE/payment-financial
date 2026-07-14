namespace Payment.Application.Common.Exceptions;

// Thrown when a payment with the same idempotency key has already been processed.
public sealed class DuplicatePaymentException : Exception
{
    public string IdempotencyKey { get; }

    // Creates an exception carrying the idempotency key that caused the duplicate.
    public DuplicatePaymentException(string idempotencyKey)
        : base($"Duplicate payment for idempotency key: {idempotencyKey}")
    {
        IdempotencyKey = idempotencyKey;
    }
}

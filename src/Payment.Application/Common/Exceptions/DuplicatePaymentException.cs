namespace Payment.Application.Common.Exceptions;

public sealed class DuplicatePaymentException : Exception
{
    public string IdempotencyKey { get; }

    public DuplicatePaymentException(string idempotencyKey)
        : base($"Duplicate payment for idempotency key: {idempotencyKey}")
    {
        IdempotencyKey = idempotencyKey;
    }
}

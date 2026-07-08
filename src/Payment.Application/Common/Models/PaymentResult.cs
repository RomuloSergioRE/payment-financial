namespace Payment.Application.Common.Models;

public sealed record PaymentResult(
    bool Success,
    string GatewayPaymentId,
    string? GatewayMessage,
    string? RawResponse);

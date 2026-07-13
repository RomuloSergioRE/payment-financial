using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Payment.Application.Features.Payments.Commands.CancelPayment;
using Payment.Application.Features.Payments.Commands.ProcessPayment;
using Payment.Application.Features.Payments.Queries.GetPayment;

namespace Payment.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentsController(ISender sender) => _sender = sender;

    [HttpPost]
    [EnableRateLimiting("Payment")]
    [ProducesResponseType(typeof(ProcessPaymentResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] ProcessPaymentRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.Items["UserId"] as Guid?;
        if (userId is null)
            return Unauthorized(new { error = "Invalid token" });

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { error = "Idempotency-Key header is required" });

        var command = new ProcessPaymentCommand(
            userId.Value,
            request.PlanType,
            request.Amount,
            request.Currency,
            request.PaymentMethod,
            idempotencyKey,
            request.CardNumber,
            request.CardCvv,
            request.CardExpiryMonth,
            request.CardExpiryYear,
            request.CardHolderName);

        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetPaymentResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPayment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.Items["UserId"] as Guid?;
        if (userId is null)
            return Unauthorized(new { error = "Invalid token" });

        var query = new GetPaymentQuery(id, userId.Value);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("Strict")]
    [ProducesResponseType(typeof(CancelPaymentResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> CancelPayment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.Items["UserId"] as Guid?;
        if (userId is null)
            return Unauthorized(new { error = "Invalid token" });

        var command = new CancelPaymentCommand(id, userId.Value);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }
}

public sealed record ProcessPaymentRequestDto(
    string PlanType,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string? CardNumber,
    string? CardCvv,
    int? CardExpiryMonth,
    int? CardExpiryYear,
    string? CardHolderName);

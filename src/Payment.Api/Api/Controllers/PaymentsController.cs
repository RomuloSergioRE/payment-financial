using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Payment.Api.Api.Extensions;
using Payment.Application.Features.Payments.Commands.CancelPayment;
using Payment.Application.Features.Payments.Commands.ProcessPayment;
using Payment.Application.Features.Payments.Commands.RefundPayment;
using Payment.Application.Features.Payments.Queries.GetPayment;
using Payment.Application.Features.Payments.Queries.ListPayments;

namespace Payment.Api.Controllers;

// REST API controller for payment operations including creation, retrieval,
// listing, cancellation and refund. All endpoints require JWT authentication.
[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentsController(ISender sender) => _sender = sender;

    // Processes a new payment with idempotency via the Idempotency-Key header.
    [HttpPost]
    [EnableRateLimiting("UserPayment")]
    [ProducesResponseType(typeof(ProcessPaymentResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] ProcessPaymentRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();
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

    // Retrieves a single payment by ID, scoped to the authenticated user.
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetPaymentResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPayment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token" });

        var query = new GetPaymentQuery(id, userId.Value);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    // Lists payments for the authenticated user with pagination and optional status filter.
    [HttpGet]
    [ProducesResponseType(typeof(ListPaymentsResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ListPayments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var userId = HttpContext.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token" });

        var query = new ListPaymentsQuery(userId.Value, page, pageSize, status);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    // Cancels a pending or active payment by ID.
    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("Strict")]
    [ProducesResponseType(typeof(CancelPaymentResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> CancelPayment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token" });

        var command = new CancelPaymentCommand(id, userId.Value);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    // Issues a refund for a completed payment, with an optional reason.
    [HttpPost("{id:guid}/refund")]
    [EnableRateLimiting("Strict")]
    [ProducesResponseType(typeof(RefundPaymentResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> RefundPayment(
        Guid id,
        [FromBody] RefundPaymentRequestDto? request = null,
        CancellationToken cancellationToken = default)
    {
        var userId = HttpContext.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token" });

        var command = new RefundPaymentCommand(id, userId.Value, request?.Reason);
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

public sealed record RefundPaymentRequestDto(
    string? Reason = null);

using System.Text.Json;
using FluentValidation;
using Payment.Application.Common.Exceptions;
using Payment.Domain.Exceptions;

namespace Payment.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";

            var errors = ex.Errors.Select(e => new
            {
                field = e.PropertyName,
                message = e.ErrorMessage
            });

            await WriteJsonAsync(context, new
            {
                error = "Validation failed",
                details = errors
            });
        }
        catch (DuplicatePaymentException ex)
        {
            _logger.LogWarning(ex, "Duplicate payment detected");
            context.Response.StatusCode = 409;
            context.Response.ContentType = "application/json";
            await WriteJsonAsync(context, new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            context.Response.StatusCode = 404;
            context.Response.ContentType = "application/json";
            await WriteJsonAsync(context, new { error = ex.Message });
        }
        catch (PaymentException ex)
        {
            _logger.LogWarning(ex, "Payment error");
            context.Response.StatusCode = 422;
            context.Response.ContentType = "application/json";
            await WriteJsonAsync(context, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var isDev = context.RequestServices
                .GetRequiredService<IWebHostEnvironment>()
                .IsDevelopment();

            var response = isDev
                ? new { error = "Internal server error", detail = ex.Message }
                : (object)new { error = "Internal server error" };

            await WriteJsonAsync(context, response);
        }
    }

    private static async Task WriteJsonAsync(HttpContext context, object value)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await context.Response.WriteAsync(json);
    }
}

using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Payment.Api.Middleware;
using Payment.Application.Common.Exceptions;
using Payment.Domain.Exceptions;

namespace Payment.UnitTests.Api;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock = new();
    private readonly DefaultHttpContext _httpContext;

    public ExceptionHandlingMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        _httpContext.RequestServices = new ServiceCollection()
            .AddSingleton<IWebHostEnvironment>(Mock.Of<IWebHostEnvironment>())
            .BuildServiceProvider();
    }

    [Fact]
    public async Task ValidationException_Returns400WithDetails()
    {
        var errors = new List<ValidationFailure>
        {
            new("Amount", "Amount must be greater than 0"),
            new("Currency", "Currency is required")
        };
        var exception = new ValidationException(errors);

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw exception, _loggerMock.Object);

        await middleware.InvokeAsync(_httpContext);

        _httpContext.Response.StatusCode.Should().Be(400);
        _httpContext.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task NotFoundException_Returns404()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("Payment", Guid.NewGuid()),
            _loggerMock.Object);

        await middleware.InvokeAsync(_httpContext);

        _httpContext.Response.StatusCode.Should().Be(404);
        _httpContext.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task DuplicatePaymentException_Returns409()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new DuplicatePaymentException("test-key"),
            _loggerMock.Object);

        await middleware.InvokeAsync(_httpContext);

        _httpContext.Response.StatusCode.Should().Be(409);
        _httpContext.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task PaymentException_Returns422()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new PaymentException("Business rule violation"),
            _loggerMock.Object);

        await middleware.InvokeAsync(_httpContext);

        _httpContext.Response.StatusCode.Should().Be(422);
        _httpContext.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task UnhandledException_Returns500()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Something went wrong"),
            _loggerMock.Object);

        await middleware.InvokeAsync(_httpContext);

        _httpContext.Response.StatusCode.Should().Be(500);
        _httpContext.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task NoException_CallsNext()
    {
        var nextCalled = false;
        var middleware = new ExceptionHandlingMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object);

        await middleware.InvokeAsync(_httpContext);

        nextCalled.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().Be(200);
    }
}

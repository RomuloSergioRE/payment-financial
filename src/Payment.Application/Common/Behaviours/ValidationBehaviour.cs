using FluentValidation;
using MediatR;

namespace Payment.Application.Common.Behaviours;

// Runs all FluentValidation validators for the request before the handler executes.
// Positioned early in the pipeline so invalid requests never reach business logic.
public sealed class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    // Executes all registered validators; short-circuits if none exist or throws on failures.
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // No validators registered — skip validation and proceed.
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        // Any validation failure prevents the handler from running.
        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}

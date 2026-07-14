using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Payment.Application;

// Registers all Application layer services: MediatR handlers, FluentValidation validators,
// and pipeline behaviors (transaction, logging, validation, performance, outbox, domain events, caching).
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR handlers from this assembly.
        services.AddMediatR(config =>
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Register FluentValidation validators from this assembly.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Register MediatR pipeline behaviors (order matters — first registered = outermost).
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.Behaviours.TransactionBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.Behaviours.LoggingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.Behaviours.ValidationBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.Behaviours.PerformanceBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.Behaviours.OutboxBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.Behaviours.DomainEventDispatcherBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.Behaviours.CachingBehavior<,>));

        return services;
    }
}

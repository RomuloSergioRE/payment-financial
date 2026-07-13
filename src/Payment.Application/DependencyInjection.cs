using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Payment.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(config =>
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.Behaviours.TransactionBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.Behaviours.LoggingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.Behaviours.ValidationBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.Behaviours.PerformanceBehaviour<,>));

        return services;
    }
}

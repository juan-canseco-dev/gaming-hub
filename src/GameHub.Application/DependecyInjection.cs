using FluentValidation;
using GameHub.Application.Abstractions.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace GameHub.Application;

public static class DependecyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblies(typeof(DependecyInjection).Assembly);
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependecyInjection).Assembly);

        return services;
    }
}

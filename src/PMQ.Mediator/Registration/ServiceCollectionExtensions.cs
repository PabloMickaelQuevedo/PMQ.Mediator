using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PMQ.Mediator;

/// <summary>
/// Extension methods for registering PMQ.Mediator services into an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the mediator and all related services (handlers, validators, pipeline behaviors) into the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for <see cref="PmqMediatorOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPmqMediator(this IServiceCollection services, Action<PmqMediatorOptions>? configure = null)
    {
        var options = new PmqMediatorOptions();
        configure?.Invoke(options);

        var assemblies = AssemblyScanner.ResolveAssemblies(options);

        // Register IMediator / ISender / IPublisher
        services.TryAdd(new ServiceDescriptor(typeof(IMediator), typeof(MediatorImplementation), options.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(ISender), sp => sp.GetRequiredService<IMediator>(), options.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IPublisher), sp => sp.GetRequiredService<IMediator>(), options.Lifetime));

        // Scan and register single handlers (request + stream)
        foreach (var (serviceType, implementationType) in AssemblyScanner.FindSingleHandlers(assemblies))
            services.TryAddTransient(serviceType, implementationType);

        // Scan and register notification handlers (multiple per notification)
        foreach (var (serviceType, implementationType) in AssemblyScanner.FindNotificationHandlers(assemblies))
            services.AddTransient(serviceType, implementationType);

        // Scan and register validators
        foreach (var (serviceType, implementationType) in AssemblyScanner.FindValidators(assemblies))
            services.TryAddTransient(serviceType, implementationType);

        // Configure FluentValidation culture
        if (options.ValidatorCulture is not null)
            ValidatorOptions.Global.LanguageManager.Culture = options.ValidatorCulture;

        // Register pipeline behaviors
        if (options.UseValidationBehavior)
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            if (options.ValidationFailureHandlerType is not null)
                services.TryAddTransient(typeof(IValidationFailureHandler<>), options.ValidationFailureHandlerType);
        }

        if (options.UseLoggingBehavior)
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}

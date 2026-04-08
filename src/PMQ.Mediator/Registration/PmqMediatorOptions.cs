using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace PMQ.Mediator;

/// <summary>
/// Configuration options for the PMQ.Mediator services.
/// </summary>
public sealed class PmqMediatorOptions
{
    internal List<Assembly> Assemblies { get; } = [];
    internal List<string> AssemblyPrefixes { get; } = [];

    /// <summary>
    /// Gets or sets the service lifetime for the mediator registration. Default is <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// Gets or sets whether the automatic FluentValidation pipeline behavior is enabled.
    /// </summary>
    public bool UseValidationBehavior { get; set; }

    /// <summary>
    /// Gets or sets whether the logging pipeline behavior is enabled.
    /// </summary>
    public bool UseLoggingBehavior { get; set; }

    /// <summary>
    /// Gets or sets the type used to handle validation failures. Must implement <see cref="IValidationFailureHandler{TResponse}"/>.
    /// </summary>
    public Type? ValidationFailureHandlerType
    {
        get => _validationFailureHandlerType;
        set
        {
            if (value is not null)
            {
                var hasInterface = value.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidationFailureHandler<>));

                if (!hasInterface && !value.IsGenericTypeDefinition)
                    throw new ArgumentException(
                        $"Type '{value.FullName}' does not implement {nameof(IValidationFailureHandler<object>)}<T>.",
                        nameof(ValidationFailureHandlerType));
            }

            _validationFailureHandlerType = value;
        }
    }

    private Type? _validationFailureHandlerType;

    /// <summary>
    /// Gets or sets the culture used for FluentValidation error messages.
    /// </summary>
    public CultureInfo? ValidatorCulture { get; set; }

    /// <summary>
    /// Registers the specified assemblies for handler and validator scanning.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The options instance for chaining.</returns>
    public PmqMediatorOptions RegisterServicesFromAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        Assemblies.AddRange(assemblies);
        return this;
    }

    /// <summary>
    /// Registers the assembly containing the specified type for handler and validator scanning.
    /// </summary>
    /// <typeparam name="T">A type whose assembly will be scanned.</typeparam>
    /// <returns>The options instance for chaining.</returns>
    public PmqMediatorOptions RegisterServicesFromAssemblyContaining<T>()
    {
        Assemblies.Add(typeof(T).Assembly);
        return this;
    }

    /// <summary>
    /// Registers assembly name prefixes used to filter assemblies for scanning.
    /// </summary>
    /// <param name="prefixes">The assembly name prefixes to match.</param>
    /// <returns>The options instance for chaining.</returns>
    public PmqMediatorOptions RegisterServicesFromAssemblyPrefixes(params string[] prefixes)
    {
        ArgumentNullException.ThrowIfNull(prefixes);

        foreach (var prefix in prefixes)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Assembly prefix must not be null or whitespace.", nameof(prefixes));
        }

        AssemblyPrefixes.AddRange(prefixes);
        return this;
    }
}

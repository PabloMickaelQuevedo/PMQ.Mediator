using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace PMQ.Mediator;

/// <summary>
/// Pipeline behavior that validates incoming requests using registered FluentValidation validators.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IServiceProvider serviceProvider) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var validators = serviceProvider.GetServices<IValidator<TRequest>>().ToList();

        if (validators.Count == 0)
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        // Try to use the custom failure handler if registered
        var failureHandler = serviceProvider.GetService<IValidationFailureHandler<TResponse>>();

        if (failureHandler is not null)
            return failureHandler.HandleFailure(failures);

        // If no custom handler, throw a ValidationException
        throw new ValidationException(failures);
    }
}

using FluentValidation.Results;

namespace PMQ.Mediator;

/// <summary>
/// Defines a custom handler for validation failures, allowing transformation into a domain response.
/// </summary>
/// <typeparam name="TResponse">The type of the response to return on validation failure.</typeparam>
public interface IValidationFailureHandler<TResponse>
{
    /// <summary>
    /// Handles validation failures and returns a domain-specific response.
    /// </summary>
    /// <param name="failures">The collection of validation failures.</param>
    /// <returns>A response representing the validation failure.</returns>
    TResponse HandleFailure(IEnumerable<ValidationFailure> failures);
}

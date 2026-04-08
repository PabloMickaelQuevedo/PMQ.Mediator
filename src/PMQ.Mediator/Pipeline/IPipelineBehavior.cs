namespace PMQ.Mediator;

/// <summary>
/// Represents an async continuation for the next task to execute in the pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);

/// <summary>
/// Defines a pipeline behavior that wraps the inner handler for a request.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Handles the request within the pipeline, with the ability to invoke the next behavior or handler.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="next">The delegate for the next action in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the pipeline.</returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

namespace PMQ.Mediator;

/// <summary>
/// Represents an async stream continuation for the next handler in the stream pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of each item in the stream.</typeparam>
public delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<out TResponse>(CancellationToken cancellationToken = default);

/// <summary>
/// Defines a pipeline behavior that wraps the inner handler for a stream request.
/// </summary>
/// <typeparam name="TRequest">The type of the stream request.</typeparam>
/// <typeparam name="TResponse">The type of each item in the stream.</typeparam>
public interface IStreamPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Handles the stream request within the pipeline, with the ability to invoke the next behavior or handler.
    /// </summary>
    /// <param name="request">The incoming stream request.</param>
    /// <param name="next">The delegate for the next action in the stream pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous stream of <typeparamref name="TResponse"/>.</returns>
    IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

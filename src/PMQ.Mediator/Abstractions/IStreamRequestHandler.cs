namespace PMQ.Mediator;

/// <summary>
/// Defines a handler for a stream request that returns an <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the stream request.</typeparam>
/// <typeparam name="TResponse">The type of each item in the stream.</typeparam>
public interface IStreamRequestHandler<in TRequest, out TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    /// <summary>
    /// Handles the stream request.
    /// </summary>
    /// <param name="request">The stream request to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous stream of <typeparamref name="TResponse"/>.</returns>
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

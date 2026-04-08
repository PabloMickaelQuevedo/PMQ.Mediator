namespace PMQ.Mediator;

/// <summary>
/// Marker interface for a request that returns an asynchronous stream of <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of each item in the stream.</typeparam>
public interface IStreamRequest<out TResponse>;

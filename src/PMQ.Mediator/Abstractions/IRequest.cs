namespace PMQ.Mediator;

/// <summary>
/// Base marker interface for all request types.
/// </summary>
public interface IBaseRequest;

/// <summary>
/// Marker interface for a request that does not return a value.
/// </summary>
public interface IRequest : IBaseRequest;

/// <summary>
/// Marker interface for a request that returns a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IRequest<out TResponse> : IBaseRequest;

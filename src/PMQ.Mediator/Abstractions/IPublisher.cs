namespace PMQ.Mediator;

/// <summary>
/// Defines a publisher that dispatches notifications to multiple handlers.
/// </summary>
public interface IPublisher
{
    /// <summary>
    /// Publishes a typed notification to all registered handlers.
    /// </summary>
    /// <typeparam name="TNotification">The type of the notification.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;

    /// <summary>
    /// Publishes a notification object to all registered handlers using dynamic dispatch.
    /// </summary>
    /// <param name="notification">The notification object to publish. Must implement <see cref="INotification"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Publish(object notification, CancellationToken cancellationToken = default);
}

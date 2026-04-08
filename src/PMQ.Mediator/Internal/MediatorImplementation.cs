using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace PMQ.Mediator;

internal sealed class MediatorImplementation(IServiceProvider serviceProvider) : IMediator
{
    private static readonly ConcurrentDictionary<Type, Lazy<object>> RequestHandlerWrappers = new();
    private static readonly ConcurrentDictionary<Type, Lazy<object>> StreamHandlerWrappers = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();

        var wrapper = (RequestHandlerWrapper<TResponse>)RequestHandlerWrappers.GetOrAdd(requestType,
            static t => new Lazy<object>(() =>
            {
                var responseType = t.GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
                    .GetGenericArguments()[0];

                var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(t, responseType);
                return Activator.CreateInstance(wrapperType)!;
            })).Value;

        return wrapper.Handle(request, serviceProvider, cancellationToken);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        var requestType = request.GetType();

        var wrapper = (VoidRequestHandlerWrapper)RequestHandlerWrappers.GetOrAdd(requestType,
            static t => new Lazy<object>(() =>
            {
                var wrapperType = typeof(VoidRequestHandlerWrapperImpl<>).MakeGenericType(t);
                return Activator.CreateInstance(wrapperType)!;
            })).Value;

        return wrapper.Handle(request, serviceProvider, cancellationToken);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();

        var wrapper = (StreamRequestHandlerWrapper<TResponse>)StreamHandlerWrappers.GetOrAdd(requestType,
            static t => new Lazy<object>(() =>
            {
                var responseType = t.GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamRequest<>))
                    .GetGenericArguments()[0];

                var wrapperType = typeof(StreamRequestHandlerWrapperImpl<,>).MakeGenericType(t, responseType);
                return Activator.CreateInstance(wrapperType)!;
            })).Value;

        return wrapper.Handle(request, serviceProvider, cancellationToken);
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        var handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>();

        foreach (var handler in handlers)
            await handler.Handle(notification, cancellationToken);
    }

    public async Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (notification is not INotification)
            throw new ArgumentException($"The object of type {notification.GetType()} does not implement {nameof(INotification)}.", nameof(notification));

        var notificationType = notification.GetType();
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
        var handleMethod = handlerType.GetMethod(nameof(INotificationHandler<INotification>.Handle))
            ?? throw new InvalidOperationException($"Could not find 'Handle' method on {handlerType}.");

        var handlers = serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            if (handler is null) continue;
            await (Task)handleMethod.Invoke(handler, [notification, cancellationToken])!;
        }
    }
}

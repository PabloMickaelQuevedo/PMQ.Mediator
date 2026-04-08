using Microsoft.Extensions.DependencyInjection;

namespace PMQ.Mediator;

internal abstract class StreamRequestHandlerWrapper<TResponse>
{
    public abstract IAsyncEnumerable<TResponse> Handle(IStreamRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

internal sealed class StreamRequestHandlerWrapperImpl<TRequest, TResponse> : StreamRequestHandlerWrapper<TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    public override IAsyncEnumerable<TResponse> Handle(IStreamRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<IStreamRequestHandler<TRequest, TResponse>>();
        var behaviors = serviceProvider
            .GetServices<IStreamPipelineBehavior<TRequest, TResponse>>()
            .ToArray();

        StreamHandlerDelegate<TResponse> pipeline = (ct) => handler.Handle((TRequest)request, ct);

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = pipeline;
            pipeline = (ct) => behavior.Handle((TRequest)request, next, ct);
        }

        return pipeline(cancellationToken);
    }
}

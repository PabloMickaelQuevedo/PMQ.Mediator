using Microsoft.Extensions.DependencyInjection;

namespace PMQ.Mediator;

internal abstract class VoidRequestHandlerWrapper
{
    public abstract Task Handle(IBaseRequest request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

internal sealed class VoidRequestHandlerWrapperImpl<TRequest> : VoidRequestHandlerWrapper
    where TRequest : IRequest
{
    public override async Task Handle(IBaseRequest request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest>>();
        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TRequest, Unit>>()
            .ToArray();

        RequestHandlerDelegate<Unit> pipeline = async (ct) =>
        {
            await handler.Handle((TRequest)request, ct);
            return Unit.Value;
        };

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = pipeline;
            pipeline = (ct) => behavior.Handle((TRequest)request, next, ct);
        }

        await pipeline(cancellationToken);
    }
}

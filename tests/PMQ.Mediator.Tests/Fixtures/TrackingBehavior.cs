using PMQ.Mediator;

namespace PMQ.Mediator.Tests.Fixtures;

public class TrackingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static readonly List<string> CallLog = [];

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        CallLog.Add($"Before:{typeof(TRequest).Name}");
        var response = await next(cancellationToken);
        CallLog.Add($"After:{typeof(TRequest).Name}");
        return response;
    }

    public static void Reset() => CallLog.Clear();
}

public class SecondTrackingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static readonly List<string> CallLog = [];

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        CallLog.Add($"Before2:{typeof(TRequest).Name}");
        var response = await next(cancellationToken);
        CallLog.Add($"After2:{typeof(TRequest).Name}");
        return response;
    }

    public static void Reset() => CallLog.Clear();
}

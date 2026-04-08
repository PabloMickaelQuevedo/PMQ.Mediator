using PMQ.Mediator;

namespace PMQ.Mediator.Tests.Fixtures;

// ── Request with response ──
public record Ping(string Message) : IRequest<Pong>;
public record Pong(string Reply);

public class PingHandler : IRequestHandler<Ping, Pong>
{
    public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        => Task.FromResult(new Pong($"Pong: {request.Message}"));
}

// ── Void request ──
public record DoNothing : IRequest;

public class DoNothingHandler : IRequestHandler<DoNothing>
{
    public bool WasCalled { get; private set; }

    public Task Handle(DoNothing request, CancellationToken cancellationToken)
    {
        WasCalled = true;
        return Task.CompletedTask;
    }
}

// ── Notification ──
public record OrderPlaced(int OrderId) : INotification;

public class OrderPlacedHandler1 : INotificationHandler<OrderPlaced>
{
    public static readonly List<int> ReceivedOrderIds = [];

    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        ReceivedOrderIds.Add(notification.OrderId);
        return Task.CompletedTask;
    }
}

public class OrderPlacedHandler2 : INotificationHandler<OrderPlaced>
{
    public static readonly List<int> ReceivedOrderIds = [];

    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        ReceivedOrderIds.Add(notification.OrderId);
        return Task.CompletedTask;
    }
}

// ── Stream request ──
public record GetNumbers(int Count) : IStreamRequest<int>;

public class GetNumbersHandler : IStreamRequestHandler<GetNumbers, int>
{
    public async IAsyncEnumerable<int> Handle(GetNumbers request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 1; i <= request.Count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }
}

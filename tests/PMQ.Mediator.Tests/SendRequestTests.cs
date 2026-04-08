using Microsoft.Extensions.DependencyInjection;
using PMQ.Mediator;
using PMQ.Mediator.Tests.Fixtures;
using Shouldly;

namespace PMQ.Mediator.Tests;

public class SendRequestTests
{
    [Fact]
    public async Task Send_WithTypedResponse_ReturnsHandlerResult()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator();
        services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        var result = await mediator.Send(new Ping("Hello"));

        result.ShouldNotBeNull();
        result.Reply.ShouldBe("Pong: Hello");
    }

    [Fact]
    public async Task Send_WithTypedResponse_PassesCancellationToken()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator();
        services.AddTransient<IRequestHandler<Ping, Pong>, CancellationAwarePingHandler>();

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => mediator.Send(new Ping("Hello"), cts.Token));
    }

    [Fact]
    public async Task Send_WithNoHandlerRegistered_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
            opt.RegisterServicesFromAssemblies(typeof(IMediator).Assembly));

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await Should.ThrowAsync<InvalidOperationException>(
            () => mediator.Send(new UnregisteredRequest()));
    }

    [Fact]
    public async Task Send_WithPipelineBehavior_ExecutesBehaviorAroundHandler()
    {
        TrackingBehavior<Ping, Pong>.Reset();

        var services = new ServiceCollection();
        services.AddPmqMediator();
        services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TrackingBehavior<,>));

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        var result = await mediator.Send(new Ping("Test"));

        result.Reply.ShouldBe("Pong: Test");
        TrackingBehavior<Ping, Pong>.CallLog.ShouldBe(["Before:Ping", "After:Ping"]);
    }

    [Fact]
    public async Task Send_WithMultipleBehaviors_ExecutesInRegistrationOrder()
    {
        TrackingBehavior<Ping, Pong>.Reset();
        SecondTrackingBehavior<Ping, Pong>.Reset();

        var services = new ServiceCollection();
        services.AddPmqMediator();
        services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();

        // Register two behaviors — first registered = outermost
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TrackingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SecondTrackingBehavior<,>));

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.Send(new Ping("Order"));

        // First behavior wraps second: Before1 → Before2 → Handler → After2 → After1
        TrackingBehavior<Ping, Pong>.CallLog[0].ShouldBe("Before:Ping");
        SecondTrackingBehavior<Ping, Pong>.CallLog[0].ShouldBe("Before2:Ping");
        SecondTrackingBehavior<Ping, Pong>.CallLog[1].ShouldBe("After2:Ping");
        TrackingBehavior<Ping, Pong>.CallLog[1].ShouldBe("After:Ping");
    }
}

// ── Helper types ──
file record UnregisteredRequest : IRequest<string>;

file class CancellationAwarePingHandler : IRequestHandler<Ping, Pong>
{
    public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new Pong("ok"));
    }
}

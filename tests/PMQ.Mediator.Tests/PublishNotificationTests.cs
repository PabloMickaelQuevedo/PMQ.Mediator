using Microsoft.Extensions.DependencyInjection;
using PMQ.Mediator;
using PMQ.Mediator.Tests.Fixtures;
using Shouldly;

namespace PMQ.Mediator.Tests;

public class PublishNotificationTests
{
    [Fact]
    public async Task Publish_Typed_InvokesAllHandlers()
    {
        OrderPlacedHandler1.ReceivedOrderIds.Clear();
        OrderPlacedHandler2.ReceivedOrderIds.Clear();

        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
            opt.RegisterServicesFromAssemblies(typeof(IMediator).Assembly));
        services.AddTransient<INotificationHandler<OrderPlaced>, OrderPlacedHandler1>();
        services.AddTransient<INotificationHandler<OrderPlaced>, OrderPlacedHandler2>();

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.Publish(new OrderPlaced(42));

        OrderPlacedHandler1.ReceivedOrderIds.ShouldContain(42);
        OrderPlacedHandler2.ReceivedOrderIds.ShouldContain(42);
    }

    [Fact]
    public async Task Publish_Object_InvokesAllHandlers()
    {
        OrderPlacedHandler1.ReceivedOrderIds.Clear();
        OrderPlacedHandler2.ReceivedOrderIds.Clear();

        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
            opt.RegisterServicesFromAssemblies(typeof(IMediator).Assembly));
        services.AddTransient<INotificationHandler<OrderPlaced>, OrderPlacedHandler1>();
        services.AddTransient<INotificationHandler<OrderPlaced>, OrderPlacedHandler2>();

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        object notification = new OrderPlaced(99);
        await mediator.Publish(notification);

        OrderPlacedHandler1.ReceivedOrderIds.ShouldContain(99);
        OrderPlacedHandler2.ReceivedOrderIds.ShouldContain(99);
    }

    [Fact]
    public async Task Publish_Object_NotINotification_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator();

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => mediator.Publish("not a notification"));

        ex.Message.ShouldContain("INotification");
    }

    [Fact]
    public async Task Publish_WithNoHandlers_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
            opt.RegisterServicesFromAssemblies(typeof(IMediator).Assembly));

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // No handlers registered — should not throw
        await mediator.Publish(new OrderPlaced(1));
    }

    [Fact]
    public async Task Publish_ExecutesHandlersSequentially()
    {
        var executionOrder = new List<int>();

        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
            opt.RegisterServicesFromAssemblies(typeof(IMediator).Assembly));
        services.AddTransient<INotificationHandler<OrderPlaced>>(_ => new OrderedHandler(executionOrder, 1, delay: 50));
        services.AddTransient<INotificationHandler<OrderPlaced>>(_ => new OrderedHandler(executionOrder, 2, delay: 10));

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.Publish(new OrderPlaced(1));

        // Should respect sequential execution (1 finishes before 2 starts)
        executionOrder.ShouldBe([1, 2]);
    }
}

file class OrderedHandler(List<int> log, int id, int delay) : INotificationHandler<OrderPlaced>
{
    public async Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        await Task.Delay(delay, cancellationToken);
        log.Add(id);
    }
}

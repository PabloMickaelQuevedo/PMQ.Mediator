using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PMQ.Mediator;
using PMQ.Mediator.IntegrationTests.Fakes;
using Shouldly;

namespace PMQ.Mediator.IntegrationTests;

/// <summary>
/// Tests that simulate real-world usage.
/// A single AddPmqMediator call with assembly scanning auto-discovers everything.
/// </summary>
public class MediatorIntegrationTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly FakeNotificationContext _notificationContext;

    public MediatorIntegrationTests()
    {
        var services = new ServiceCollection();

        services.AddScoped<FakeNotificationContext>();
        services.AddLogging(b => b.AddDebug());

        services.AddPmqMediator(options =>
        {
            options.RegisterServicesFromAssemblies(typeof(CreateOrderCommandHandler).Assembly);
            options.UseValidationBehavior = true;
            options.UseLoggingBehavior = true;
            options.ValidatorCulture = new CultureInfo("pt-BR");
            options.ValidationFailureHandlerType = typeof(NotificationFailureHandler<>);
        });

        _provider = services.BuildServiceProvider();
        _notificationContext = _provider.CreateScope().ServiceProvider.GetRequiredService<FakeNotificationContext>();

        // Reset static state
        CreateOrderCommandHandler.Reset();
        DeleteOrderCommandHandler.Reset();
        SendEmailOnOrderCreated.Reset();
        UpdateDashboardOnOrderCreated.Reset();
    }

    public void Dispose()
    {
        _provider.Dispose();
        ValidatorOptions.Global.LanguageManager.Culture = null;
    }

    // ─── Send with Response ───

    [Fact]
    public async Task Send_ValidCommand_ShouldPassValidation_ExecuteHandler_ReturnResult()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new CreateOrderCommand("John Doe", 100m));

        result.ShouldNotBeNull();
        result.OrderId.ShouldBe(1);
        result.Status.ShouldBe("Created");
        CreateOrderCommandHandler.WasCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Send_InvalidCommand_ShouldBlockHandler_AddNotifications()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<FakeNotificationContext>();

        var result = await mediator.Send(new CreateOrderCommand("", -5));

        // Handler should NOT have been called
        CreateOrderCommandHandler.WasCalled.ShouldBeFalse();

        // Custom failure handler should have added notifications
        context.Notifications.ShouldNotBeEmpty();
        context.Notifications.ShouldContain(n => n.Property == "CustomerName");
        context.Notifications.ShouldContain(n => n.Property == "Total");
    }

    [Fact]
    public async Task Send_PartiallyInvalidCommand_ShouldBlockHandler_AddSpecificNotification()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<FakeNotificationContext>();

        // Customer name is valid, but total is invalid
        var result = await mediator.Send(new CreateOrderCommand("Jane", 0));

        CreateOrderCommandHandler.WasCalled.ShouldBeFalse();
        result.ShouldBeNull(); // default! from failure handler

        context.Notifications.Count.ShouldBe(1);
        context.Notifications[0].Property.ShouldBe("Total");
        context.Notifications[0].Message.ShouldContain("greater than");
    }

    // ─── Send Void ───

    [Fact]
    public async Task Send_VoidCommand_ShouldExecuteHandler()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new DeleteOrderCommand(42));

        DeleteOrderCommandHandler.WasCalled.ShouldBeTrue();
    }

    // ─── Resolve ISender / IPublisher ───

    [Fact]
    public async Task ISender_ResolvedFromDI_ShouldWork()
    {
        using var scope = _provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(new CreateOrderCommand("Test", 50m));

        result.ShouldNotBeNull();
        result.Status.ShouldBe("Created");
    }

    [Fact]
    public async Task IPublisher_ResolvedFromDI_ShouldWork()
    {
        using var scope = _provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.Publish(new OrderCreatedEvent(10, "Test"));

        SendEmailOnOrderCreated.ProcessedOrders.ShouldContain(10);
        UpdateDashboardOnOrderCreated.ProcessedOrders.ShouldContain(10);
    }

    // ─── Notifications (fan-out) ───

    [Fact]
    public async Task Publish_ShouldInvokeAllHandlersDiscoveredByAssemblyScan()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Publish(new OrderCreatedEvent(77, "Alice"));

        SendEmailOnOrderCreated.ProcessedOrders.ShouldContain(77);
        UpdateDashboardOnOrderCreated.ProcessedOrders.ShouldContain(77);
    }

    [Fact]
    public async Task Publish_Object_ShouldInvokeAllHandlersDiscoveredByAssemblyScan()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        object notification = new OrderCreatedEvent(88, "Bob");
        await mediator.Publish(notification);

        SendEmailOnOrderCreated.ProcessedOrders.ShouldContain(88);
        UpdateDashboardOnOrderCreated.ProcessedOrders.ShouldContain(88);
    }

    // ─── Stream ───

    [Fact]
    public async Task CreateStream_ShouldReturnAllItemsFromHandler()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var updates = new List<string>();
        await foreach (var update in mediator.CreateStream(new GetOrderUpdatesStream(42)))
            updates.Add(update);

        updates.Count.ShouldBe(3);
        updates[0].ShouldBe("Order 42: Pending");
        updates[1].ShouldBe("Order 42: Processing");
        updates[2].ShouldBe("Order 42: Completed");
    }

    // ─── Scoped lifetime ───

    [Fact]
    public void Mediator_ShouldBeScopedByDefault()
    {
        using var scope1 = _provider.CreateScope();
        using var scope2 = _provider.CreateScope();

        var mediator1 = scope1.ServiceProvider.GetRequiredService<IMediator>();
        var mediator2 = scope2.ServiceProvider.GetRequiredService<IMediator>();

        // Different scopes should produce different instances
        mediator1.ShouldNotBeSameAs(mediator2);
    }

    [Fact]
    public void SameScope_ShouldReturnSameMediatorInstance()
    {
        using var scope = _provider.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // All interfaces should resolve to the same scoped instance
        sender.ShouldBeSameAs(mediator);
        publisher.ShouldBeSameAs(mediator);
    }

    // ─── FluentValidation Culture ───

    [Fact]
    public void ValidatorCulture_ShouldBeConfigured()
    {
        ValidatorOptions.Global.LanguageManager.Culture.ShouldNotBeNull();
        ValidatorOptions.Global.LanguageManager.Culture!.Name.ShouldBe("pt-BR");
    }

    // ─── Full pipeline order: Logging → Validation → Handler ───

    [Fact]
    public async Task FullPipeline_ValidRequest_LoggingAndValidation_ShouldExecuteInOrder()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<FakeNotificationContext>();

        var result = await mediator.Send(new CreateOrderCommand("Pipeline Test", 200m));

        // Validation passed, handler executed, result returned
        result.ShouldNotBeNull();
        result.Status.ShouldBe("Created");
        CreateOrderCommandHandler.WasCalled.ShouldBeTrue();

        // No validation notifications
        context.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task FullPipeline_InvalidRequest_ValidationShouldShortCircuit()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<FakeNotificationContext>();

        // Both fields are invalid
        var result = await mediator.Send(new CreateOrderCommand("", 0));

        // Handler should NOT be called
        CreateOrderCommandHandler.WasCalled.ShouldBeFalse();

        // Failure handler populated notification context
        context.Notifications.Count.ShouldBe(2);

        // Result is default (null)
        result.ShouldBeNull();
    }
}

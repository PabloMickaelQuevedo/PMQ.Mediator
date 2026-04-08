using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PMQ.Mediator;
using PMQ.Mediator.Tests.Fixtures;
using Shouldly;

namespace PMQ.Mediator.Tests;

public class ServiceRegistrationTests
{
    [Fact]
    public void AddPmqMediator_RegistersIMediatorAsScopedByDefault()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator();

        var descriptor = services.First(d => d.ServiceType == typeof(IMediator));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddPmqMediator_RegistersISenderAndIPublisher()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator();

        services.ShouldContain(d => d.ServiceType == typeof(ISender));
        services.ShouldContain(d => d.ServiceType == typeof(IPublisher));
    }

    [Fact]
    public void ISender_And_IPublisher_ResolveSameInstanceAsIMediator()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator();

        using var scope = services.BuildServiceProvider().CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        sender.ShouldBeSameAs(mediator);
        publisher.ShouldBeSameAs(mediator);
    }

    [Fact]
    public void AddPmqMediator_WithCustomLifetime_UsesIt()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt => opt.Lifetime = ServiceLifetime.Singleton);

        var descriptor = services.First(d => d.ServiceType == typeof(IMediator));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddPmqMediator_ScansHandlersFromSpecifiedAssembly()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
            opt.RegisterServicesFromAssemblies(typeof(PingHandler).Assembly));

        services.ShouldContain(d =>
            d.ServiceType == typeof(IRequestHandler<Ping, Pong>));
    }

    [Fact]
    public void AddPmqMediator_ScansValidatorsFromSpecifiedAssembly()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
            opt.RegisterServicesFromAssemblies(typeof(PingValidator).Assembly));

        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidator<Ping>));
    }

    [Fact]
    public void AddPmqMediator_ScansNotificationHandlersFromSpecifiedAssembly()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
            opt.RegisterServicesFromAssemblies(typeof(OrderPlacedHandler1).Assembly));

        var notifDescriptors = services
            .Where(d => d.ServiceType == typeof(INotificationHandler<OrderPlaced>))
            .ToList();

        // Should find at least OrderPlacedHandler1 and OrderPlacedHandler2
        notifDescriptors.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void AddPmqMediator_ScansStreamHandlersFromSpecifiedAssembly()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
            opt.RegisterServicesFromAssemblies(typeof(GetNumbersHandler).Assembly));

        services.ShouldContain(d =>
            d.ServiceType == typeof(IStreamRequestHandler<GetNumbers, int>));
    }

    [Fact]
    public void AddPmqMediator_WithUseValidationBehavior_RegistersValidationBehavior()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt => opt.UseValidationBehavior = true);

        services.ShouldContain(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(ValidationBehavior<,>));
    }

    [Fact]
    public void AddPmqMediator_WithUseLoggingBehavior_RegistersLoggingBehavior()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt => opt.UseLoggingBehavior = true);

        services.ShouldContain(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(LoggingBehavior<,>));
    }

    [Fact]
    public void AddPmqMediator_WithValidationFailureHandlerType_RegistersIt()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
        {
            opt.UseValidationBehavior = true;
            opt.ValidationFailureHandlerType = typeof(TestValidationFailureHandler<>);
        });

        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidationFailureHandler<>));
    }

    [Fact]
    public void AddPmqMediator_WithValidatorCulture_SetsFluentValidationCulture()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
            opt.ValidatorCulture = new CultureInfo("pt-BR"));

        ValidatorOptions.Global.LanguageManager.Culture.ShouldNotBeNull();
        ValidatorOptions.Global.LanguageManager.Culture!.Name.ShouldBe("pt-BR");

        // Reset to avoid polluting other tests
        ValidatorOptions.Global.LanguageManager.Culture = null;
    }

    [Fact]
    public void AddPmqMediator_WithoutBehaviorFlags_DoesNotRegisterBehaviors()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
            opt.RegisterServicesFromAssemblies(typeof(IMediator).Assembly));

        services.ShouldNotContain(d => d.ServiceType == typeof(IPipelineBehavior<,>));
    }

    [Fact]
    public void Options_RegisterServicesFromAssemblyContaining_AddsAssembly()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
            opt.RegisterServicesFromAssemblyContaining<PingHandler>());

        services.ShouldContain(d =>
            d.ServiceType == typeof(IRequestHandler<Ping, Pong>));
    }
}

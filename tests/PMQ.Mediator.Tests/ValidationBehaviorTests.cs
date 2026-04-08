using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PMQ.Mediator;
using PMQ.Mediator.Tests.Fixtures;
using Shouldly;

namespace PMQ.Mediator.Tests;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task WhenNoValidators_ShouldCallHandler()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt => opt.UseValidationBehavior = true);
        services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        var result = await mediator.Send(new Ping("Hello"));

        result.Reply.ShouldBe("Pong: Hello");
    }

    [Fact]
    public async Task WhenValidationPasses_ShouldCallHandler()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt => opt.UseValidationBehavior = true);
        services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
        services.AddTransient<IValidator<Ping>, PingValidator>();

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        var result = await mediator.Send(new Ping("Hi"));

        result.Reply.ShouldBe("Pong: Hi");
    }

    [Fact]
    public async Task WhenValidationFails_NoFailureHandler_ShouldThrowValidationException()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt => opt.UseValidationBehavior = true);
        services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
        services.AddTransient<IValidator<Ping>, PingValidator>();

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        var ex = await Should.ThrowAsync<ValidationException>(
            () => mediator.Send(new Ping("")));

        ex.Errors.ShouldNotBeEmpty();
        ex.Errors.First().ErrorMessage.ShouldBe("Message is required.");
    }

    [Fact]
    public async Task WhenValidationFails_WithFailureHandler_ShouldUseCustomHandler()
    {
        TestValidationFailureHandler<Pong>.Reset();

        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
        {
            opt.RegisterServicesFromAssemblies(typeof(IMediator).Assembly);
            opt.UseValidationBehavior = true;
            opt.ValidationFailureHandlerType = typeof(TestValidationFailureHandler<>);
        });
        services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
        services.AddTransient<IValidator<Ping>, PingValidator>();

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        var result = await mediator.Send(new Ping(""));

        // Custom handler returns default (null for Pong)
        result.ShouldBeNull();

        // Custom handler received the failures
        TestValidationFailureHandler<Pong>.LastFailures.ShouldNotBeNull();
        TestValidationFailureHandler<Pong>.LastFailures.Count.ShouldBe(1);
        TestValidationFailureHandler<Pong>.LastFailures[0].ErrorMessage.ShouldBe("Message is required.");
    }

    [Fact]
    public async Task WhenMultipleValidatorsFail_ShouldAggregateAllFailures()
    {
        TestValidationFailureHandler<Pong>.Reset();

        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
        {
            opt.RegisterServicesFromAssemblies(typeof(IMediator).Assembly);
            opt.UseValidationBehavior = true;
            opt.ValidationFailureHandlerType = typeof(TestValidationFailureHandler<>);
        });
        services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
        services.AddTransient<IValidator<Ping>, PingValidator>();
        services.AddTransient<IValidator<Ping>, PingMaxLengthValidator>();

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Long message: passes NotEmpty but fails MaxLength(10)
        var result = await mediator.Send(new Ping("This is a very long message"));

        TestValidationFailureHandler<Pong>.LastFailures.ShouldNotBeNull();
        TestValidationFailureHandler<Pong>.LastFailures.ShouldNotBeEmpty();
        TestValidationFailureHandler<Pong>.LastFailures.ShouldContain(f =>
            f.ErrorMessage == "Message must be at most 10 characters.");
    }

    [Fact]
    public async Task WhenBothValidatorsFail_ShouldAggregateAllFailures()
    {
        TestValidationFailureHandler<Pong>.Reset();

        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
        {
            opt.RegisterServicesFromAssemblies(typeof(IMediator).Assembly);
            opt.UseValidationBehavior = true;
            opt.ValidationFailureHandlerType = typeof(TestValidationFailureHandler<>);
        });
        services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
        services.AddTransient<IValidator<Ping>, PingValidator>();
        services.AddTransient<IValidator<Ping>, PingMaxLengthValidator>();

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Empty message: fails NotEmpty from PingValidator
        var result = await mediator.Send(new Ping(""));

        TestValidationFailureHandler<Pong>.LastFailures.ShouldNotBeNull();
        TestValidationFailureHandler<Pong>.LastFailures.Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task WhenValidationFails_HandlerShouldNotBeCalled()
    {
        var handlerCalled = false;

        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
        {
            opt.RegisterServicesFromAssemblies(typeof(IMediator).Assembly);
            opt.UseValidationBehavior = true;
            opt.ValidationFailureHandlerType = typeof(TestValidationFailureHandler<>);
        });
        services.AddTransient<IRequestHandler<Ping, Pong>>(_ => new SpyPingHandler(() => handlerCalled = true));
        services.AddTransient<IValidator<Ping>, PingValidator>();

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.Send(new Ping(""));

        handlerCalled.ShouldBeFalse();
    }
}

file class SpyPingHandler(Action onCalled) : IRequestHandler<Ping, Pong>
{
    public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
    {
        onCalled();
        return Task.FromResult(new Pong("spy"));
    }
}

using Microsoft.Extensions.DependencyInjection;
using PMQ.Mediator;
using PMQ.Mediator.Tests.Fixtures;
using Shouldly;

namespace PMQ.Mediator.Tests;

public class SendVoidRequestTests
{
    [Fact]
    public async Task Send_VoidRequest_ExecutesHandler()
    {
        var handler = new DoNothingHandler();

        var services = new ServiceCollection();
        services.AddPmqMediator();
        services.AddSingleton<IRequestHandler<DoNothing>>(handler);

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.Send(new DoNothing());

        handler.WasCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Send_VoidRequest_WithBehavior_ExecutesBehavior()
    {
        var log = new List<string>();

        var services = new ServiceCollection();
        services.AddPmqMediator();
        services.AddSingleton<IRequestHandler<DoNothing>, DoNothingHandler>();
        services.AddTransient<IPipelineBehavior<DoNothing, Unit>>(_ => new InlineVoidBehavior(log));

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.Send(new DoNothing());

        log.ShouldBe(["Before", "After"]);
    }

    [Fact]
    public async Task Send_VoidRequest_WithNoHandler_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
            opt.RegisterServicesFromAssemblies(typeof(IMediator).Assembly));

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await Should.ThrowAsync<InvalidOperationException>(
            () => mediator.Send(new DoNothing()));
    }
}

file class InlineVoidBehavior(List<string> log) : IPipelineBehavior<DoNothing, Unit>
{
    public async Task<Unit> Handle(DoNothing request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken)
    {
        log.Add("Before");
        var result = await next(cancellationToken);
        log.Add("After");
        return result;
    }
}

using Microsoft.Extensions.DependencyInjection;
using PMQ.Mediator;
using PMQ.Mediator.Tests.Fixtures;
using Shouldly;

namespace PMQ.Mediator.Tests;

public class CreateStreamTests
{
    [Fact]
    public async Task CreateStream_ReturnsAllItems()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator();
        services.AddTransient<IStreamRequestHandler<GetNumbers, int>, GetNumbersHandler>();

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        var results = new List<int>();

        await foreach (var number in mediator.CreateStream(new GetNumbers(5)))
            results.Add(number);

        results.ShouldBe([1, 2, 3, 4, 5]);
    }

    [Fact]
    public async Task CreateStream_WithNoHandler_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
            opt.RegisterServicesFromAssemblies(typeof(IMediator).Assembly));

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in mediator.CreateStream(new GetNumbers(1))) { }
        });
    }

    [Fact]
    public async Task CreateStream_WithStreamBehavior_ExecutesBehavior()
    {
        var log = new List<string>();

        var services = new ServiceCollection();
        services.AddPmqMediator();
        services.AddTransient<IStreamRequestHandler<GetNumbers, int>, GetNumbersHandler>();
        services.AddTransient<IStreamPipelineBehavior<GetNumbers, int>>(
            _ => new TrackingStreamBehavior(log));

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        var results = new List<int>();
        await foreach (var number in mediator.CreateStream(new GetNumbers(3)))
            results.Add(number);

        results.ShouldBe([1, 2, 3]);
        log.ShouldContain("Wrapping");
    }
}

file class TrackingStreamBehavior(List<string> log) : IStreamPipelineBehavior<GetNumbers, int>
{
    public async IAsyncEnumerable<int> Handle(GetNumbers request, StreamHandlerDelegate<int> next,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        log.Add("Wrapping");
        await foreach (var item in next(cancellationToken))
            yield return item;
    }
}

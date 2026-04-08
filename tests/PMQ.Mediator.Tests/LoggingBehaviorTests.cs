using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PMQ.Mediator;
using PMQ.Mediator.Tests.Fixtures;
using Shouldly;

namespace PMQ.Mediator.Tests;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task WhenRequestSucceeds_ShouldLogStartAndEnd()
    {
        var logger = new FakeLogger<LoggingBehavior<Ping, Pong>>();

        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
        {
            opt.UseLoggingBehavior = true;
        });
        services.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
        services.AddSingleton<ILogger<LoggingBehavior<Ping, Pong>>>(logger);

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        var result = await mediator.Send(new Ping("Log test"));

        result.Reply.ShouldBe("Pong: Log test");

        logger.LogEntries.Count.ShouldBe(2);
        logger.LogEntries[0].Level.ShouldBe(LogLevel.Information);
        logger.LogEntries[0].Message.ShouldContain("Handling Ping");
        logger.LogEntries[1].Level.ShouldBe(LogLevel.Information);
        logger.LogEntries[1].Message.ShouldContain("Handled Ping");
        logger.LogEntries[1].Message.ShouldContain("ms");
    }

    [Fact]
    public async Task WhenRequestThrows_ShouldLogError()
    {
        var logger = new FakeLogger<LoggingBehavior<Ping, Pong>>();

        var services = new ServiceCollection();
        services.AddPmqMediator(opt =>
        {
            opt.UseLoggingBehavior = true;
        });
        services.AddTransient<IRequestHandler<Ping, Pong>>(_ => new ThrowingPingHandler());
        services.AddSingleton<ILogger<LoggingBehavior<Ping, Pong>>>(logger);

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await Should.ThrowAsync<InvalidOperationException>(
            () => mediator.Send(new Ping("Fail")));

        logger.LogEntries.Count.ShouldBe(2);
        logger.LogEntries[0].Level.ShouldBe(LogLevel.Information);
        logger.LogEntries[0].Message.ShouldContain("Handling Ping");
        logger.LogEntries[1].Level.ShouldBe(LogLevel.Error);
        logger.LogEntries[1].Message.ShouldContain("Error handling Ping");
    }
}

file class ThrowingPingHandler : IRequestHandler<Ping, Pong>
{
    public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Boom");
}

file class FakeLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> LogEntries { get; } = [];

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        LogEntries.Add((logLevel, formatter(state, exception)));
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}

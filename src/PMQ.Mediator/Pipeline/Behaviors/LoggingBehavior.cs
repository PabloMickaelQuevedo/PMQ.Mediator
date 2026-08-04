using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace PMQ.Mediator;

/// <summary>
/// Pipeline behavior that logs request handling, including execution time and errors.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;

        logger.HandlingRequest(requestName);

        var timestamp = Stopwatch.GetTimestamp();

        try
        {
            var response = await next(cancellationToken);
            var elapsed = ElapsedMilliseconds(timestamp);

            logger.HandledRequest(requestName, elapsed);

            return response;
        }
        catch (Exception exception)
        {
            var elapsed = ElapsedMilliseconds(timestamp);

            logger.RequestFailed(exception, requestName, elapsed);
            throw;
        }
    }

    private static long ElapsedMilliseconds(long timestamp)
        => (long)Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
}

/// <summary>
/// Compile-time generated log messages for <see cref="LoggingBehavior{TRequest, TResponse}"/>.
/// </summary>
/// <remarks>
/// Source-generated logging skips argument boxing and message formatting when the level is
/// disabled — relevant here because this behavior runs on every single request.
/// </remarks>
internal static partial class LoggingBehaviorLogs
{
    [LoggerMessage(EventId = 100, Level = LogLevel.Information, Message = "Handling {RequestName}")]
    public static partial void HandlingRequest(this ILogger logger, string requestName);

    [LoggerMessage(EventId = 101, Level = LogLevel.Information, Message = "Handled {RequestName} in {ElapsedMilliseconds}ms")]
    public static partial void HandledRequest(this ILogger logger, string requestName, long elapsedMilliseconds);

    [LoggerMessage(EventId = 102, Level = LogLevel.Error, Message = "Error handling {RequestName} after {ElapsedMilliseconds}ms")]
    public static partial void RequestFailed(this ILogger logger, Exception exception, string requestName, long elapsedMilliseconds);
}

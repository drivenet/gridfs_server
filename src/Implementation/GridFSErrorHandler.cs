using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using MongoDB.Driver;

namespace GridFSServer.Implementation;

internal sealed class GridFSErrorHandler : IGridFSErrorHandler
{
    private static readonly Action<ILogger, string, string, Exception?> LogRetry =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            EventIds.Retry,
            "Retrying after {Action} for file \"{Filename}\"");

    private readonly ILogger _logger;

    public GridFSErrorHandler(ILogger<GridFSErrorHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResult> HandleErrors<TResult>(
        Func<Task<TResult>> action,
        string filename,
        Func<bool> retryValidator,
        CancellationToken cancellationToken)
    {
        const int Attempts = 5;
        const int DelayBetweenAttemptsMs = 1700;
        var tries = Attempts;
        while (true)
        {
            if (tries <= 0)
            {
                return await action();
            }

            try
            {
                return await action();
            }
            catch (TimeoutException exception)
            {
                LogRetry(_logger, "read timeout", filename, exception);
            }
            catch (MongoWaitQueueFullException exception)
            {
                LogRetry(_logger, "wait queue exception", filename, exception);
            }
            catch (MongoConnectionPoolPausedException exception)
            {
                LogRetry(_logger, "connection pool paused", filename, exception);
            }
            catch (MongoNodeIsRecoveringException exception)
            {
                LogRetry(_logger, "node is recovering", filename, exception);
            }
            catch (MongoConnectionException exception) when (retryValidator())
            {
                LogRetry(_logger, "network error", filename, exception);
            }
            catch (MongoCommandException exception) when (retryValidator())
            {
                LogRetry(_logger, "protocol error", filename, exception);
            }

            --tries;
            await Task.Delay(DelayBetweenAttemptsMs, cancellationToken);
        }
    }

    private static class EventIds
    {
        public static readonly EventId Retry = new(1, nameof(Retry));
    }
}

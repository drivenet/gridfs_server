using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using MongoDB.Driver;

namespace GridFSServer.Implementation
{
    internal sealed class GridFSErrorHandler : IGridFSErrorHandler
    {
        private readonly ILogger _logger;

        public GridFSErrorHandler(ILogger<GridFSErrorHandler> logger)
        {
            _logger = logger;
        }

        public async Task<TResult> HandleErrors<TResult>(Func<Task<TResult>> action, string filename, CancellationToken cancellationToken)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            const int Attempts = 4;
            const int DelayBetweenAttemptsMs = 1500;
            var tries = Attempts;
            while (true)
            {
                try
                {
                    return await action();
                }
                catch (TimeoutException exception) when (tries > 1)
                {
                    _logger?.LogWarning(exception, "Retrying after read timeout for file \"{0}\".", filename);
                }
                catch (MongoConnectionException exception) when (tries > 1)
                {
                    _logger?.LogWarning(exception, "Retrying after network error for file \"{0}\".", filename);
                }
                catch (MongoCommandException exception) when (tries > 1)
                {
                    _logger?.LogWarning(exception, "Retrying after protocol error for file \"{0}\".", filename);
                }

                --tries;
                await Task.Delay(DelayBetweenAttemptsMs, cancellationToken);
            }
        }
    }
}

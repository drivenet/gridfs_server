using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GridFSServer.Middleware
{
    internal sealed class StatisticsMiddleware : IDisposable
    {
        private const int MinCode = 100;

        private const int MaxCode = 599;

        private const int CodeRange = MaxCode + 1 - MinCode;

        private static readonly TimeSpan TraceInterval = TimeSpan.FromMinutes(1);

        private readonly int[] _counts = new int[CodeRange];

        private readonly StringBuilder _stats = new StringBuilder();

        private readonly RequestDelegate _next;

        private readonly ILogger<StatisticsMiddleware> _logger;

        private readonly Timer _timer;

        public StatisticsMiddleware(RequestDelegate next, ILogger<StatisticsMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timer = new Timer(LogStatistics, null, TraceInterval, TraceInterval);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            await _next.Invoke(httpContext);

            const int ConnectionTimedOut = 522;
            var statusCode = httpContext.RequestAborted.IsCancellationRequested
                ? ConnectionTimedOut
                : httpContext.Response.StatusCode;
            var index = statusCode - MinCode;

            if (index >= 0 && index < CodeRange)
            {
                Interlocked.Increment(ref _counts[index]);
            }
        }

        private void LogStatistics(object u1)
        {
            try
            {
                LogStatistics();
            }
#pragma warning disable CA1031 // Do not catch general exception types -- critical diagnostics path
            catch
            {
#if DEBUG
                throw;
#endif
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        private void LogStatistics()
        {
            _stats.Clear();
            for (var i = 0; i < CodeRange; i++)
            {
                var count = _counts[i];
                if (count == 0)
                {
                    continue;
                }

                if (_stats.Length != 0)
                {
                    _stats.Append(' ');
                }

                _stats.AppendFormat(CultureInfo.InvariantCulture, "code{0}={1}", i + MinCode, count);
                Interlocked.Add(ref _counts[i], -count);
            }

            if (_stats.Length != 0)
            {
                _logger.LogTrace(_stats.ToString());
            }
        }
    }
}

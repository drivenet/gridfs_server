using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GridFSServer.Middleware
{
    internal sealed class StatisticsMiddleware
    {
        private const int MinCode = 100;

        private const int MaxCode = 599;

        private const int CodeRange = MaxCode + 1 - MinCode;

        private readonly RequestDelegate _next;

        private readonly ILogger<StatisticsMiddleware> _logger;

        private readonly int[] _counts = new int[CodeRange];

        private long _nextTime;

        public StatisticsMiddleware(RequestDelegate next, ILogger<StatisticsMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            await _next.Invoke(httpContext).ConfigureAwait(false);
            var index = httpContext.Response.StatusCode - MinCode;
            if (index >= 0 && index < CodeRange)
            {
                Interlocked.Increment(ref _counts[index]);
            }

            var time = Stopwatch.GetTimestamp();
            var nextTime = _nextTime;
            if (time > nextTime)
            {
                time += Stopwatch.Frequency * 60;
                if (Interlocked.CompareExchange(ref _nextTime, time, nextTime) == nextTime && nextTime != 0)
                {
                    var stats = new StringBuilder();
                    for (int i = 0; i < CodeRange; i++)
                    {
                        var count = _counts[i];
                        if (count == 0)
                        {
                            continue;
                        }

                        if (stats.Length != 0)
                        {
                            stats.Append(' ');
                        }

                        stats.AppendFormat(CultureInfo.InvariantCulture, "code{0}={1}", i + MinCode, count);
                        Interlocked.Add(ref _counts[i], -count);
                    }

                    _logger.LogTrace(stats.ToString());
                }
            }
        }
    }
}

using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GridFSServer.Middleware;

internal sealed class StatisticsMiddleware : IMiddleware, IDisposable
{
    private const int MinCode = 100;
    private const int MaxCode = 599;
    private const int CodeRange = MaxCode + 1 - MinCode;
    private static readonly TimeSpan TraceInterval = TimeSpan.FromMinutes(1);
    private static readonly Action<ILogger, string, Exception?> Log =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            EventIds.Statistics,
            "{Statistics}");

    private readonly int[] _counts = new int[CodeRange];
    private readonly StringBuilder _stats = new();
    private readonly ILogger<StatisticsMiddleware> _logger;
    private readonly ITimer _timer;

    public StatisticsMiddleware(ILogger<StatisticsMiddleware> logger, TimeProvider timeProvider)
    {
        if (timeProvider is null)
        {
            throw new ArgumentNullException(nameof(timeProvider));
        }

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timer = timeProvider.CreateTimer(LogStatistics, null, TraceInterval, TraceInterval);
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        await next.Invoke(context);
        const int ConnectionTimedOut = 522;
        var statusCode = context.RequestAborted.IsCancellationRequested
            ? ConnectionTimedOut
            : context.Response.StatusCode;
        var index = statusCode - MinCode;
        if (index >= 0 && index < CodeRange)
        {
            Interlocked.Increment(ref _counts[index]);
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    private void LogStatistics(object? u1)
    {
        var lockTaken = false;
        try
        {
            Monitor.TryEnter(_timer, ref lockTaken);
            if (lockTaken)
            {
                LogStatistics();
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types -- critical diagnostics path
        catch
        {
#if DEBUG
            throw;
#endif
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(_timer);
            }
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
            Log(_logger, _stats.ToString(), null);
        }
    }

    private static class EventIds
    {
        public static readonly EventId Statistics = new(1, nameof(Statistics));
    }
}

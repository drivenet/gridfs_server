using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using GridFSServer.Components;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GridFSServer.Implementation;

internal sealed class LoggingFileServer : IHttpFileServer
{
    private static readonly Action<ILogger, HostString, PathString, IPAddress?, bool, long?, Exception?> LogServed =
        LoggerMessage.Define<HostString, PathString, IPAddress?, bool, long?>(
            LogLevel.Information,
            EventIds.Served,
            "{Host}{Path} {IpAddress} {Success} {Length}");

    private readonly IHttpFileServer _inner;
    private readonly IOptionsMonitor<HttpServerOptions> _options;
    private readonly ILogger _logger;

    public LoggingFileServer(IHttpFileServer inner, IOptionsMonitor<HttpServerOptions> options, ILogger<IHttpFileServer> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> TryServeFile(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var success = await _inner.TryServeFile(httpContext, cancellationToken);
        if (_options.CurrentValue.LogRequests)
        {
            long? length;
            try
            {
                length = httpContext.Response.Body.Length;
            }
            catch (NotSupportedException)
            {
                length = null;
            }

            LogServed(
                _logger,
                httpContext.Request.Host,
                httpContext.Request.Path,
                httpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress,
                success,
                length,
                null);
        }

        return success;
    }

    private static class EventIds
    {
        public static readonly EventId Served = new(1, nameof(Served));
    }
}

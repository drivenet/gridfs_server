using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace GridFSServer.Middleware;

internal sealed class ReverseProxyMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        UpdateRequestId(context);
        UpdateRemoteIpAddress(context);
        UpdateServerPort(context);
        UpdateConnectionId(context);
        UpdateScheme(context);

        await next(context);
    }

    private static void UpdateRemoteIpAddress(HttpContext context)
    {
        if (context.Request.Headers["X-Real-IP"] is [string header]
            && IPAddress.TryParse(header, out var address)
            && (address.AddressFamily == AddressFamily.InterNetwork || address.AddressFamily == AddressFamily.InterNetworkV6))
        {
            var connection = context.Connection;
            connection.RemoteIpAddress = address;
            connection.RemotePort = 0;
        }
    }

    private static void UpdateServerPort(HttpContext context)
    {
        if (context.Request.Headers["X-Forwarded-Port"] is [string header]
            && ushort.TryParse(header, NumberStyles.None, NumberFormatInfo.InvariantInfo, out var port)
            && port != 0)
        {
            var connection = context.Connection;
            connection.LocalIpAddress = IPAddress.Any;
            connection.LocalPort = port;
        }
    }

    private static void UpdateScheme(HttpContext context)
    {
        var request = context.Request;
        if (request.Headers["X-Forwarded-Proto"] is [string header])
        {
            switch (header)
            {
                case "http":
                    request.Scheme = Uri.UriSchemeHttp;
                    break;

                case "https":
                    request.Scheme = Uri.UriSchemeHttps;
                    break;
            }
        }
    }

    private static void UpdateConnectionId(HttpContext context)
    {
        if (context.Request.Headers["X-Connection-ID"] is [string header]
            && !string.IsNullOrWhiteSpace(header))
        {
            var connection = context.Connection;
            var id = connection.Id;
            if (!string.IsNullOrWhiteSpace(id))
            {
                id = header + ":" + id;
            }

            connection.Id = id;
        }
    }

    private static void UpdateRequestId(HttpContext context)
    {
        if (context.Request.Headers["X-Request-ID"] is [string header]
            && !string.IsNullOrWhiteSpace(header))
        {
            context.TraceIdentifier = header;
        }
    }
}

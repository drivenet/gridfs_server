using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace GridFSServer.Middleware
{
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
            if (context.Request.Headers.TryGetValue("X-Real-IP", out var header)
                && header.Count == 1
                && IPAddress.TryParse(header[0], out var address)
                && (address.AddressFamily == AddressFamily.InterNetwork || address.AddressFamily == AddressFamily.InterNetworkV6))
            {
                var connection = context.Connection;
                connection.RemoteIpAddress = address;
                connection.RemotePort = 0;
            }
        }

        private static void UpdateServerPort(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Forwarded-Port", out var header)
                && header.Count == 1
                && ushort.TryParse(header[0], NumberStyles.None, NumberFormatInfo.InvariantInfo, out var port)
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
            if (request.Headers.TryGetValue("X-Forwarded-Proto", out var header)
                && header.Count == 1)
            {
                switch (header[0])
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
            string value;
            if (context.Request.Headers.TryGetValue("X-Connection-ID", out var header)
                && header.Count == 1
                && !string.IsNullOrWhiteSpace(value = header[0]))
            {
                var connection = context.Connection;
                var id = connection.Id;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    id = value + ":" + id;
                }

                connection.Id = id;
            }
        }

        private static void UpdateRequestId(HttpContext context)
        {
            string value;
            if (context.Request.Headers.TryGetValue("X-Request-ID", out var header)
                && header.Count == 1
                && !string.IsNullOrWhiteSpace(value = header[0]))
            {
                context.TraceIdentifier = value;
            }
        }
    }
}

using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace GridFSServer.Middleware
{
    internal sealed class ReverseProxyMiddleware
    {
        private readonly RequestDelegate _next;

        public ReverseProxyMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            UpdateRequestId(httpContext);
            UpdateRemoteIpAddress(httpContext);
            UpdateServerPort(httpContext);
            UpdateConnectionId(httpContext);
            UpdateScheme(httpContext);

            return _next(httpContext);
        }

        private static void UpdateRemoteIpAddress(HttpContext httpContext)
        {
            if (httpContext.Request.Headers.TryGetValue("X-Real-IP", out var header)
                && header.Count == 1
                && IPAddress.TryParse(header[0], out var address)
                && (address.AddressFamily == AddressFamily.InterNetwork || address.AddressFamily == AddressFamily.InterNetworkV6))
            {
                var connection = httpContext.Connection;
                connection.RemoteIpAddress = address;
                connection.RemotePort = 0;
            }
        }

        private static void UpdateServerPort(HttpContext httpContext)
        {
            if (httpContext.Request.Headers.TryGetValue("X-Forwarded-Port", out var header)
                && header.Count == 1
                && ushort.TryParse(header[0], NumberStyles.None, NumberFormatInfo.InvariantInfo, out var port)
                && port != 0)
            {
                var connection = httpContext.Connection;
                connection.LocalIpAddress = IPAddress.Any;
                connection.LocalPort = port;
            }
        }

        private static void UpdateScheme(HttpContext httpContext)
        {
            var request = httpContext.Request;
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

        private static void UpdateConnectionId(HttpContext httpContext)
        {
            string value;
            if (httpContext.Request.Headers.TryGetValue("X-Connection-ID", out var header)
                && header.Count == 1
                && !string.IsNullOrWhiteSpace(value = header[0]))
            {
                var connection = httpContext.Connection;
                var id = connection.Id;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    id = value + ":" + id;
                }

                connection.Id = id;
            }
        }

        private static void UpdateRequestId(HttpContext httpContext)
        {
            string value;
            if (httpContext.Request.Headers.TryGetValue("X-Request-ID", out var header)
                && header.Count == 1
                && !string.IsNullOrWhiteSpace(value = header[0]))
            {
                httpContext.TraceIdentifier = value;
            }
        }
    }
}

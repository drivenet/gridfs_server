using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

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

            var requestIdentifierFeature = httpContext.Features.Get<IHttpRequestIdentifierFeature>();
            if (requestIdentifierFeature != null)
            {
                UpdateRequestId(httpContext, requestIdentifierFeature);
            }

            var connectionFeature = httpContext.Features.Get<IHttpConnectionFeature>();
            if (connectionFeature != null)
            {
                UpdateRemoteIpAddress(httpContext, connectionFeature);
                UpdateServerPort(httpContext, connectionFeature);
                UpdateConnectionId(httpContext, connectionFeature);
            }

            var requestFeature = httpContext.Features.Get<IHttpRequestFeature>();
            if (requestFeature != null)
            {
                UpdateScheme(httpContext, requestFeature);
            }

            return _next(httpContext);
        }

        private static void UpdateRemoteIpAddress(HttpContext httpContext, IHttpConnectionFeature connectionFeature)
        {
            if (httpContext.Request.Headers.TryGetValue("X-Real-IP", out var header)
                && header.Count == 1
                && IPAddress.TryParse(header[0], out var address)
                && (address.AddressFamily == AddressFamily.InterNetwork || address.AddressFamily == AddressFamily.InterNetworkV6))
            {
                connectionFeature.RemoteIpAddress = address;
                connectionFeature.RemotePort = 0;
            }
        }

        private static void UpdateServerPort(HttpContext httpContext, IHttpConnectionFeature connectionFeature)
        {
            if (httpContext.Request.Headers.TryGetValue("X-Forwarded-Port", out var header)
                && header.Count == 1
                && ushort.TryParse(header[0], NumberStyles.None, NumberFormatInfo.InvariantInfo, out var port)
                && port != 0)
            {
                connectionFeature.LocalIpAddress = IPAddress.Any;
                connectionFeature.LocalPort = port;
            }
        }

        private static void UpdateScheme(HttpContext httpContext, IHttpRequestFeature requestFeature)
        {
            if (httpContext.Request.Headers.TryGetValue("X-Forwarded-Proto", out var header)
                && header.Count == 1)
            {
                switch (header[0])
                {
                    case "http":
                        requestFeature.Scheme = "http";
                        break;

                    case "https":
                        requestFeature.Scheme = "https";
                        break;
                }
            }
        }

        private static void UpdateConnectionId(HttpContext httpContext, IHttpConnectionFeature connectionFeature)
        {
            string value;
            if (httpContext.Request.Headers.TryGetValue("X-Connection-ID", out var header)
                && header.Count == 1
                && !string.IsNullOrWhiteSpace(value = header[0]))
            {
                var id = connectionFeature.ConnectionId;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    id = value + ":" + id;
                }

                connectionFeature.ConnectionId = id;
            }
        }

        private static void UpdateRequestId(HttpContext httpContext, IHttpRequestIdentifierFeature requestIdentifierFeature)
        {
            string value;
            if (httpContext.Request.Headers.TryGetValue("X-Request-ID", out var header)
                && header.Count == 1
                && !string.IsNullOrWhiteSpace(value = header[0]))
            {
                requestIdentifierFeature.TraceIdentifier = value;
            }
        }
    }
}

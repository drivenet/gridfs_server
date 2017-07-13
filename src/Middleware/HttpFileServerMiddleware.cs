using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace GridFSServer.Middleware
{
    internal sealed class HttpFileServerMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly Components.IHttpFileServer _fileServer;

        public HttpFileServerMiddleware(RequestDelegate next, Components.IHttpFileServer fileServer)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _fileServer = fileServer ?? throw new ArgumentNullException(nameof(fileServer));
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (!CheckMethod(httpContext.Request.Method, out var serveContent))
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                return;
            }

            var cancellationToken = httpContext.RequestAborted;
            try
            {
                if (await _fileServer.TryServeFile(httpContext, serveContent, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }
            }
            catch (OperationCanceledException exception) when (exception.CancellationToken == cancellationToken)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                return;
            }

            await _next.Invoke(httpContext).ConfigureAwait(false);
        }

        private static bool CheckMethod(string method, out bool serveContent)
        {
            switch (method)
            {
                case "GET":
                    serveContent = true;
                    return true;

                case "HEAD":
                    serveContent = false;
                    return true;

                default:
                    serveContent = false;
                    return false;
            }
        }
    }
}

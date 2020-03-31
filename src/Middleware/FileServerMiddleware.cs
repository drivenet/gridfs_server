using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace GridFSServer.Middleware
{
    internal sealed class FileServerMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly Components.IHttpFileServer _fileServer;

        public FileServerMiddleware(RequestDelegate next, Components.IHttpFileServer fileServer)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _fileServer = fileServer ?? throw new ArgumentNullException(nameof(fileServer));
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext is null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (!CheckMethod(httpContext.Request.Method, out var serveContent))
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                return;
            }

            try
            {
                if (await _fileServer.TryServeFile(httpContext, serveContent, httpContext.RequestAborted))
                {
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                if (httpContext.Response.HasStarted)
                {
                    httpContext.Abort();
                }
                else
                {
                    httpContext.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                }

                return;
            }

            await _next.Invoke(httpContext);
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

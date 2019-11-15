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

        public async Task InvokeAsync(HttpContext httpContext)
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

            try
            {
                if (await _fileServer.TryServeFileAsync(httpContext, serveContent, httpContext.RequestAborted))
                {
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
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

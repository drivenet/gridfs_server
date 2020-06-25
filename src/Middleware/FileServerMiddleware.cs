using System;
using System.Threading;
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

            try
            {
                if (await _fileServer.TryServeFile(httpContext, CancellationToken.None))
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
                    httpContext.Response.StatusCode = StatusCodes.Status408RequestTimeout;
                }

                return;
            }

            await _next.Invoke(httpContext);
        }
    }
}

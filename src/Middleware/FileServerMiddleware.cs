using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace GridFSServer.Middleware;

internal sealed class FileServerMiddleware : IMiddleware
{
    private readonly Components.IHttpFileServer _fileServer;

    public FileServerMiddleware(Components.IHttpFileServer fileServer)
    {
        _fileServer = fileServer ?? throw new ArgumentNullException(nameof(fileServer));
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            if (await _fileServer.TryServeFile(context, CancellationToken.None))
            {
                return;
            }
        }
        catch (OperationCanceledException)
        {
            if (context.Response.HasStarted)
            {
                context.Abort();
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status408RequestTimeout;
            }

            return;
        }

        await next.Invoke(context);
    }
}

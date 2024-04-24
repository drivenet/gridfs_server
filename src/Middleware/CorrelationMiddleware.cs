using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace GridFSServer.Middleware;

internal sealed class CorrelationMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var requestId = context.TraceIdentifier;
        if (!string.IsNullOrWhiteSpace(requestId))
        {
            context.Response.Headers["X-Request-ID"] = requestId;
        }

        await next(context);
    }
}

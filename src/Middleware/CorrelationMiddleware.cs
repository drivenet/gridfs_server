using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace GridFSServer.Middleware;

internal sealed class CorrelationMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var requestId = context.Features.Get<IHttpRequestIdentifierFeature>()?.TraceIdentifier;
        if (!string.IsNullOrWhiteSpace(requestId))
        {
            context.Response.Headers.Add("X-Request-ID", requestId);
        }

        await next(context);
    }
}

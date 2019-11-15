using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace GridFSServer.Components
{
    internal interface IHttpFileServer
    {
        Task<bool> TryServeFileAsync(HttpContext httpContext, bool serveContent, CancellationToken cancellationToken);
    }
}

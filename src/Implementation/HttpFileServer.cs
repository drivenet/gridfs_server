using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace GridFSServer.Implementation
{
    internal sealed class HttpFileServer : Components.IHttpFileServer
    {
        private readonly Components.IFileSourceResolver _fileSourceResolver;

        private readonly IContentTypeProvider _contentTypeProvider;

        private readonly IOptionsMonitor<Components.HttpServerOptions> _optionsSource;

        // IOptionsMonitor<> is used instead of IOptionsSnapshot<> injection to improve performance and prevent memory leaks
        public HttpFileServer(Components.IFileSourceResolver fileSourceResolver, IContentTypeProvider contentTypeProvider, IOptionsMonitor<Components.HttpServerOptions> optionsSource)
        {
            _fileSourceResolver = fileSourceResolver ?? throw new ArgumentNullException(nameof(fileSourceResolver));
            _contentTypeProvider = contentTypeProvider ?? throw new ArgumentNullException(nameof(contentTypeProvider));
            _optionsSource = optionsSource ?? throw new ArgumentNullException(nameof(optionsSource));
        }

        public async Task<bool> TryServeFile(HttpContext httpContext, CancellationToken cancellationToken)
        {
            if (httpContext is null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (!CheckMethod(httpContext.Request.Method, out var serveContent))
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                return true;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, httpContext.RequestAborted);
            var request = httpContext.Request;
            var fileSource = _fileSourceResolver.Resolve(request.Host);
            var filename = request.Path.ToString().TrimStart('/');
            using var fileInfo = await fileSource.FetchFile(filename, cts.Token);
            if (fileInfo is null)
            {
                return false;
            }

            return await ServeFile(httpContext.Response, fileInfo, serveContent, cts.Token);
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

        private static async Task<bool> ServeBody(HttpResponse response, Components.IFileInfo fileInfo, CancellationToken cancellationToken)
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (fileInfo is null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }

            var stream = response.Body;
            if (!await fileInfo.CopyTo(stream, cancellationToken))
            {
                if (stream.CanSeek)
                {
                    response.Clear();
                }

                return false;
            }

            return true;
        }

        private async Task<bool> ServeFile(HttpResponse response, Components.IFileInfo fileInfo, bool serveContent, CancellationToken cancellationToken)
        {
            ServeHeaders(response, fileInfo.Filename);
            if (!serveContent)
            {
                return true;
            }

            return await ServeBody(response, fileInfo, cancellationToken);
        }

        private void ServeHeaders(HttpResponse response, string filename)
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (_contentTypeProvider.TryGetContentType(filename, out var contentType))
            {
                response.ContentType = contentType;
            }

            var options = _optionsSource.CurrentValue;
            var cacheControl = options?.CacheControl;
            if (cacheControl is object)
            {
                response.Headers.Add(HeaderNames.CacheControl, cacheControl);
            }
        }
    }
}

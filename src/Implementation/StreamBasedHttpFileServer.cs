using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace GridFSServer.Implementation
{
    internal sealed class StreamBasedHttpFileServer : Components.IHttpFileServer
    {
        private readonly Components.IFileSourceResolver _fileSourceResolver;

        private readonly IContentTypeProvider _contentTypeProvider;

        private readonly IOptionsMonitor<Components.HttpServerOptions> _optionsSource;

        // IOptionsMonitor<> is used instead of IOptionsSnapshot<> injection to improve performance and prevent memory leaks
        public StreamBasedHttpFileServer(Components.IFileSourceResolver fileSourceResolver, IContentTypeProvider contentTypeProvider, IOptionsMonitor<Components.HttpServerOptions> optionsSource)
        {
            _fileSourceResolver = fileSourceResolver ?? throw new ArgumentNullException(nameof(fileSourceResolver));
            _contentTypeProvider = contentTypeProvider ?? throw new ArgumentNullException(nameof(contentTypeProvider));
            _optionsSource = optionsSource ?? throw new ArgumentNullException(nameof(optionsSource));
        }

        public async Task<bool> TryServeFile(HttpContext httpContext, bool serveContent, CancellationToken cancellationToken)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var request = httpContext.Request;
            var fileSource = _fileSourceResolver.Resolve(request.Host);
            var filename = request.Path.ToString().TrimStart('/');
            using (var fileInfo = await fileSource.FetchFile(filename, cancellationToken).ConfigureAwait(false))
            {
                if (fileInfo == null)
                {
                    return false;
                }

                return await ServeFile(httpContext.Response, fileInfo, serveContent, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<bool> ServeBody(HttpResponse response, Components.IFileInfo fileInfo, CancellationToken cancellationToken)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (fileInfo == null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }

            var stream = response.Body;
            if (!await fileInfo.CopyTo(stream, cancellationToken).ConfigureAwait(false))
            {
                if (stream.CanSeek)
                {
                    response.Clear();
                }

                return false;
            }

            return true;
        }

        private Task<bool> ServeFile(HttpResponse response, Components.IFileInfo fileInfo, bool serveContent, CancellationToken cancellationToken)
        {
            ServeHeaders(response, fileInfo?.Filename);
            if (!serveContent)
            {
                return Task.FromResult(true);
            }

            return ServeBody(response, fileInfo, cancellationToken);
        }

        private void ServeHeaders(HttpResponse response, string filename)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (_contentTypeProvider.TryGetContentType(filename, out var contentType))
            {
                response.ContentType = contentType;
            }

            var options = _optionsSource.CurrentValue;
            var cacheControl = options?.CacheControl;
            if (cacheControl != null)
            {
                response.Headers.Add(HeaderNames.CacheControl, cacheControl);
            }
        }
    }
}

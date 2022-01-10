using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;

namespace GridFSServer.Implementation;

internal sealed class HttpFileServer : Components.IHttpFileServer
{
    private readonly Components.IFileSourceResolver _fileSourceResolver;
    private readonly IContentTypeProvider _contentTypeProvider;
    private readonly IOptionsMonitor<Components.HttpServerOptions> _optionsSource;
    private readonly RecyclableMemoryStreamManager _streamManager;

    public HttpFileServer(
        Components.IFileSourceResolver fileSourceResolver,
        IContentTypeProvider contentTypeProvider,
        IOptionsMonitor<Components.HttpServerOptions> optionsSource,
        RecyclableMemoryStreamManager streamManager)
    {
        _fileSourceResolver = fileSourceResolver ?? throw new ArgumentNullException(nameof(fileSourceResolver));
        _contentTypeProvider = contentTypeProvider ?? throw new ArgumentNullException(nameof(contentTypeProvider));
        _optionsSource = optionsSource ?? throw new ArgumentNullException(nameof(optionsSource));
        _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
    }

    public async Task<bool> TryServeFile(HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (!CheckMethod(httpContext.Request.Method, out var serveContent))
        {
            httpContext.Response.StatusCode = StatusCodes.Status501NotImplemented;
            return true;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, httpContext.RequestAborted);
        var request = httpContext.Request;
        var fileSource = _fileSourceResolver.Resolve(request.Host);
        var filename = request.Path.ToString().TrimStart('/');
        await using var fileInfo = await fileSource.FetchFile(filename, cts.Token);
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

    private async Task<bool> ServeBody(HttpResponse response, Components.IFileInfo fileInfo, Components.HttpServerOptions options, CancellationToken cancellationToken)
    {
        var length = fileInfo.Length;
        if (length > options.MaxBufferedLength)
        {
            return await fileInfo.CopyTo(response.Body, cancellationToken);
        }

        await using var buffer = (RecyclableMemoryStream)_streamManager.GetStream();
        if (!await fileInfo.CopyTo(buffer, cancellationToken))
        {
            return false;
        }

        response.ContentLength = length;
        foreach (var segment in buffer.GetReadOnlySequence())
        {
            await response.BodyWriter.WriteAsync(segment, cancellationToken);
        }

        return true;
    }

    private async Task<bool> ServeFile(HttpResponse response, Components.IFileInfo fileInfo, bool serveContent, CancellationToken cancellationToken)
    {
        var options = _optionsSource.CurrentValue;
        ServeHeaders(response, fileInfo.Filename, options);
        if (!serveContent)
        {
            response.Headers.ContentLength = fileInfo.Length;
            return true;
        }

        return await ServeBody(response, fileInfo, options, cancellationToken);
    }

    private void ServeHeaders(HttpResponse response, string filename, Components.HttpServerOptions options)
    {
        if (_contentTypeProvider.TryGetContentType(filename, out var contentType))
        {
            response.ContentType = contentType;
        }

        if (options.CacheControl is { } cacheControl)
        {
            response.Headers.Add(HeaderNames.CacheControl, cacheControl);
        }
    }
}

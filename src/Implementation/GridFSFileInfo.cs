using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver.GridFS;

namespace GridFSServer.Implementation;

internal sealed class GridFSFileInfo : Components.IFileInfo
{
    private readonly IGridFSErrorHandler _errorHandler;
    private readonly Func<CancellationToken, Task<GridFSDownloadStream<BsonValue>?>> _streamFactory;
    private GridFSDownloadStream<BsonValue>? _stream;

    public GridFSFileInfo(
        GridFSDownloadStream<BsonValue> stream,
        Func<CancellationToken, Task<GridFSDownloadStream<BsonValue>?>> streamFactory,
        IGridFSErrorHandler errorHandler)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _streamFactory = streamFactory ?? throw new ArgumentNullException(nameof(streamFactory));
        Filename = stream.FileInfo.Filename;
        Length = stream.FileInfo.Length;
    }

    public string Filename { get; }

    public long Length { get; }

    public ValueTask DisposeAsync()
        => _stream is { } stream
            ? stream.DisposeAsync()
            : ValueTask.CompletedTask;

    public Task<bool> CopyTo(Stream stream, CancellationToken cancellationToken)
    {
        const int MinBufferSize = 81920;
        const int MaxBufferSize = 1 << 20;
        return _errorHandler.HandleErrors(
            Copy,
            Filename,
            () => stream.CanSeek || _stream is not null,
            cancellationToken);

        async Task<bool> Copy()
        {
            if (_stream is null)
            {
                _stream = await _streamFactory(cancellationToken);
                if (_stream is null)
                {
                    return false;
                }
            }

            var position = stream.CanSeek ? stream.Position : 0;
            var bufferSize = Math.Min(Math.Max(_stream.FileInfo.ChunkSizeBytes, MinBufferSize), MaxBufferSize);
            try
            {
                await _stream.CopyToAsync(stream, bufferSize, cancellationToken);
            }
            catch (GridFSChunkException) when (position == 0 && _stream.Position == 0)
            {
                return false;
            }
            catch (GridFSChunkException) when (stream.CanSeek)
            {
                stream.SetLength(position);
                return false;
            }
            catch when (stream.CanSeek)
            {
                stream.SetLength(position);
                await _stream.DisposeAsync();
                _stream = null;
                throw;
            }
            catch
            {
                await _stream.DisposeAsync();
                _stream = null;
                throw;
            }

            return true;
        }
    }
}

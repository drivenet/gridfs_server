using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver.GridFS;

namespace GridFSServer.Implementation
{
    internal sealed class GridFSFileInfo : Components.IFileInfo
    {
        private readonly IGridFSErrorHandler _errorHandler;

        private readonly GridFSDownloadStream<BsonValue> _stream;

        public GridFSFileInfo(GridFSDownloadStream<BsonValue> stream, IGridFSErrorHandler errorHandler)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        public string Filename => _stream.FileInfo.Filename;

        public void Dispose() => _stream.Dispose();

        public Task<bool> CopyTo(Stream stream, CancellationToken cancellationToken)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            const int MinBufferSize = 81920;
            const int MaxBufferSize = 1 << 20;
            var bufferSize = Math.Min(Math.Max(_stream.FileInfo.ChunkSizeBytes, MinBufferSize), MaxBufferSize);
            return _errorHandler.HandleErrors(
                Copy,
                Filename,
                () => (stream.CanSeek && _stream.CanSeek) || _stream.Position == 0,
                cancellationToken);

            async Task<bool> Copy()
            {
                var position = stream.CanSeek ? stream.Position : 0;
                try
                {
                    if (_stream.Position != 0)
                    {
                        _stream.Position = 0;
                    }

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
                    throw;
                }

                return true;
            }
        }
    }
}

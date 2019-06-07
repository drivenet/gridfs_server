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
        private readonly IGridFSBucket<BsonValue> _bucket;

        private readonly IGridFSErrorHandler _errorHandler;

        private GridFSDownloadStream<BsonValue> _stream;

        public GridFSFileInfo(GridFSDownloadStream<BsonValue> stream, IGridFSBucket<BsonValue> bucket, IGridFSErrorHandler errorHandler)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _bucket = bucket ?? throw new ArgumentNullException(nameof(bucket));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        public string Filename => _stream.FileInfo.Filename;

        public Task<bool> CopyTo(Stream stream, CancellationToken cancellationToken)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return stream.CanSeek
                ? _errorHandler.HandleErrors(() => CopyToImpl(stream, cancellationToken), _stream.FileInfo.Filename, cancellationToken)
                : CopyToImpl(stream, cancellationToken);
        }

        public void Dispose() => _stream.Dispose();

        private async Task<bool> CopyToImpl(Stream stream, CancellationToken cancellationToken)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (_stream.Position != 0)
            {
                var newStream = await _bucket.OpenDownloadStreamAsync(_stream.FileInfo.Id, null, cancellationToken);
                if (newStream == null)
                {
                    return false;
                }

                using (_stream)
                {
                    _stream = newStream;
                }
            }

            const int MinBufferSize = 81920;
            const int MaxBufferSize = 1 << 20;
            var bufferSize = Math.Min(Math.Max(_stream.FileInfo.ChunkSizeBytes, MinBufferSize), MaxBufferSize);
            var position = stream.CanSeek ? stream.Position : 0;
            try
            {
                await _stream.CopyToAsync(stream, bufferSize, cancellationToken);
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

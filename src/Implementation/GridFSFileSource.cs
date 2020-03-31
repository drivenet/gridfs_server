﻿using System;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver.GridFS;

namespace GridFSServer.Implementation
{
    internal sealed class GridFSFileSource : Components.IFileSource
    {
        private readonly IGridFSBucket<BsonValue> _bucket;

        private readonly IGridFSErrorHandler _errorHandler;

        public GridFSFileSource(IGridFSBucket<BsonValue> bucket, IGridFSErrorHandler errorHandler)
        {
            _bucket = bucket ?? throw new ArgumentNullException(nameof(bucket));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        public async Task<Components.IFileInfo> FetchFile(string filename, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return null;
            }

            var stream = await _errorHandler.HandleErrors(
                () => FetchStream(filename, cancellationToken),
                filename,
                () => true,
                cancellationToken);

            if (stream is null)
            {
                return null;
            }

            return new GridFSFileInfo(stream, _errorHandler);
        }

        private async Task<GridFSDownloadStream<BsonValue>> FetchStream(string filename, CancellationToken cancellationToken)
        {
            try
            {
                return await _bucket.OpenDownloadStreamByNameAsync(
                    filename,
                    new GridFSDownloadByNameOptions { Seekable = true },
                    cancellationToken);
            }
            catch (GridFSFileNotFoundException)
            {
                return null;
            }
        }
    }
}

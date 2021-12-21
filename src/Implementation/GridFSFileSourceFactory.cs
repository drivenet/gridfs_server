using System;

using Microsoft.IO;

using MongoDB.Bson;
using MongoDB.Driver.GridFS;

namespace GridFSServer.Implementation
{
    internal sealed class GridFSFileSourceFactory : IGridFSFileSourceFactory
    {
        private readonly IGridFSErrorHandler _errorHandler;
        private readonly RecyclableMemoryStreamManager _streamManager;

        public GridFSFileSourceFactory(IGridFSErrorHandler errorHandler, RecyclableMemoryStreamManager streamManager)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        }

        public Components.IFileSource Create(IGridFSBucket<BsonValue> bucket)
        {
            var source = new GridFSFileSource(bucket, _errorHandler);
            source.Initialize();
            return new BufferingFileSource(source, _streamManager);
        }
    }
}

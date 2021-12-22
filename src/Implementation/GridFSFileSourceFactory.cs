using System;

using MongoDB.Bson;
using MongoDB.Driver.GridFS;

namespace GridFSServer.Implementation;

internal sealed class GridFSFileSourceFactory : IGridFSFileSourceFactory
{
    private readonly IGridFSErrorHandler _errorHandler;

    public GridFSFileSourceFactory(IGridFSErrorHandler errorHandler)
    {
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
    }

    public Components.IFileSource Create(IGridFSBucket<BsonValue> bucket)
    {
        var source = new GridFSFileSource(bucket, _errorHandler);
        source.Initialize();
        return source;
    }
}

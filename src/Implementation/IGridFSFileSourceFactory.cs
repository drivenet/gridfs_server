using MongoDB.Bson;
using MongoDB.Driver.GridFS;

namespace GridFSServer.Implementation;

internal interface IGridFSFileSourceFactory
{
    Components.IFileSource Create(IGridFSBucket<BsonValue> bucket);
}

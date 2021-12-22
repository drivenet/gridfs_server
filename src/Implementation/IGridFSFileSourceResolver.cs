using MongoDB.Driver;

namespace GridFSServer.Implementation;

internal interface IGridFSFileSourceResolver
{
    Components.IFileSource Resolve(MongoUrl url);
}

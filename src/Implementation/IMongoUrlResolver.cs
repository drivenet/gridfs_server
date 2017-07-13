using Microsoft.AspNetCore.Http;

using MongoDB.Driver;

namespace GridFSServer.Implementation
{
    internal interface IMongoUrlResolver
    {
        MongoUrl Resolve(HostString host);
    }
}

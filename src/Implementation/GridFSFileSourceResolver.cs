using System;
using System.Collections.Generic;
using System.Linq;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace GridFSServer.Implementation;

internal sealed class GridFSFileSourceResolver : IGridFSFileSourceResolver
{
    private static readonly IEnumerable<ReadPreference> ReadPreferences =
        new[]
        {
            ReadPreference.Nearest,
            ReadPreference.PrimaryPreferred,
        };

    private readonly IGridFSFileSourceFactory _fileSourceFactory;

    public GridFSFileSourceResolver(IGridFSFileSourceFactory fileSourceFactory)
    {
        _fileSourceFactory = fileSourceFactory ?? throw new ArgumentNullException(nameof(fileSourceFactory));
    }

    public Components.IFileSource Resolve(MongoUrl url)
    {
        var client = new MongoClient(url);
        var database = client.GetDatabase(url.DatabaseName);
        Components.IFileSource fileSource;
        if (url.ReadPreference is null)
        {
            var servers = ReadPreferences
                .Select(readPreference => CreateFileSource(database.WithReadPreference(readPreference)))
                .ToArray();
            fileSource = new CompositeFileSource(servers);
        }
        else
        {
            fileSource = CreateFileSource(database);
        }

        return new DisposingFileSource(fileSource, client);
    }

    private Components.IFileSource CreateFileSource(IMongoDatabase database) => _fileSourceFactory.Create(new GridFSBucket<BsonValue>(database));
}

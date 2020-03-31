using System;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

using MongoDB.Driver;

namespace GridFSServer.Implementation
{
    internal sealed class ConfigBasedMongoUrlResolver : IMongoUrlResolver
    {
        private readonly IConfiguration _configuration;

        public ConfigBasedMongoUrlResolver(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public MongoUrl Resolve(HostString host)
        {
            var connectionString = _configuration.GetConnectionString(host.ToString());
            if (connectionString is null)
            {
                return null;
            }

            return new MongoUrl(connectionString);
        }
    }
}

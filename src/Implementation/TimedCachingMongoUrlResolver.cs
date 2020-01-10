using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

using Microsoft.AspNetCore.Http;

using MongoDB.Driver;

namespace GridFSServer.Implementation
{
    internal sealed class TimedCachingMongoUrlResolver : IMongoUrlResolver
    {
        private readonly ConcurrentDictionary<HostString, MongoUrl> _cache = new ConcurrentDictionary<HostString, MongoUrl>();

        private readonly Func<HostString, MongoUrl> _resolver;

        private long _nextTime;

        public TimedCachingMongoUrlResolver(IMongoUrlResolver inner)
        {
            _resolver = (inner ?? throw new ArgumentNullException(nameof(inner))).Resolve;
        }

        public MongoUrl Resolve(HostString host)
        {
            var time = Stopwatch.GetTimestamp();
            var nextTime = _nextTime;

            if (time > nextTime)
            {
                time += Stopwatch.Frequency;
                if (Interlocked.CompareExchange(ref _nextTime, time, nextTime) == nextTime)
                {
                    _cache.Clear();
                }
            }

            return _cache.GetOrAdd(host, _resolver);
        }
    }
}

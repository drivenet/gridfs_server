using System;
using System.Collections.Concurrent;
using System.Threading;

using Microsoft.AspNetCore.Http;

using MongoDB.Driver;

namespace GridFSServer.Implementation;

internal sealed class TimedCachingMongoUrlResolver : IMongoUrlResolver
{
    private readonly ConcurrentDictionary<HostString, MongoUrl?> _cache = new();

    private readonly TimeProvider _timeProvider;

    private readonly Func<HostString, MongoUrl?> _resolver;

    private long _nextTime;

    public TimedCachingMongoUrlResolver(IMongoUrlResolver inner, TimeProvider timeProvider)
    {
        _resolver = (inner ?? throw new ArgumentNullException(nameof(inner))).Resolve;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public MongoUrl? Resolve(HostString host)
    {
        var time = _timeProvider.GetTimestamp();
        var nextTime = _nextTime;
        if (time > nextTime)
        {
            time += _timeProvider.TimestampFrequency;
            if (Interlocked.CompareExchange(ref _nextTime, time, nextTime) == nextTime)
            {
                _cache.Clear();
            }
        }

        return _cache.GetOrAdd(host, _resolver);
    }
}

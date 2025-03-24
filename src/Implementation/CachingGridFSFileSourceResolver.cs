using System;
using System.Collections.Concurrent;
using System.Threading;

using MongoDB.Driver;

namespace GridFSServer.Implementation;

internal sealed class CachingGridFSFileSourceResolver : IGridFSFileSourceResolver, IDisposable
{
    private readonly ConcurrentDictionary<MongoUrl, CacheEntry> _cache = new();

    private readonly TimeProvider _timeProvider;

    private readonly IGridFSFileSourceResolver _inner;

    private readonly Func<MongoUrl, CacheEntry> _cacheFactory;

    private long _nextTime;

    public CachingGridFSFileSourceResolver(IGridFSFileSourceResolver inner, TimeProvider timeProvider)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _cacheFactory = CreateEntry;
    }

    public Components.IFileSource Resolve(MongoUrl url)
    {
        var time = _timeProvider.GetTimestamp();
        var nextTime = _nextTime;
        if (time > nextTime)
        {
            const int CacheCleanIntervalSeconds = 59;
            time += _timeProvider.TimestampFrequency * CacheCleanIntervalSeconds;
            if (Interlocked.CompareExchange(ref _nextTime, time, nextTime) == nextTime)
            {
                CleanCache(url);
            }
        }

        return _cache.GetOrAdd(url, _cacheFactory).Value;
    }

    private void CleanCache(MongoUrl key)
    {
        foreach (var pair in _cache)
        {
            if (pair.Key != key && pair.Value.ResetRefs())
            {
                if (_cache.TryRemove(pair.Key, out var entry))
                {
                    entry.Dispose();
                }
            }
        }
    }

    void IDisposable.Dispose()
    {
        foreach (var pair in _cache)
        {
            pair.Value.Dispose();
        }
    }

    private CacheEntry CreateEntry(MongoUrl url)
        => new(() => _inner.Resolve(url));

    private sealed class CacheEntry
    {
        private readonly Lazy<Components.IFileSource> _value;

        private volatile int _hasRefs;

        public CacheEntry(Func<Components.IFileSource> valueFactory)
        {
            _value = new Lazy<Components.IFileSource>(valueFactory);
        }

        public Components.IFileSource Value
        {
            get
            {
                var value = _value.Value;
                _hasRefs = 1;
                return value;
            }
        }

        public bool ResetRefs() => Interlocked.CompareExchange(ref _hasRefs, 0, 1) == 0;

        public void Dispose()
        {
            if (_value.IsValueCreated
                && _value.Value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}

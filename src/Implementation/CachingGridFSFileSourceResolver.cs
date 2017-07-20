using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

using MongoDB.Driver;

namespace GridFSServer.Implementation
{
    internal sealed class CachingGridFSFileSourceResolver : IGridFSFileSourceResolver
    {
        private readonly ConcurrentDictionary<MongoUrl, CacheEntry> _cache = new ConcurrentDictionary<MongoUrl, CacheEntry>();

        private readonly IGridFSFileSourceResolver _inner;

        private readonly Func<MongoUrl, CacheEntry> _cacheFactory;

        private long _nextTime;

        public CachingGridFSFileSourceResolver(IGridFSFileSourceResolver inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _cacheFactory = CreateEntry;
        }

        public Components.IFileSource Resolve(MongoUrl url)
        {
            var time = Stopwatch.GetTimestamp();
            var nextTime = _nextTime;
            if (time > nextTime)
            {
                const int CacheCleanIntervalSeconds = 59;
                time += Stopwatch.Frequency * CacheCleanIntervalSeconds;
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
                    _cache.TryRemove(pair.Key, out _);
                }
            }
        }

        private CacheEntry CreateEntry(MongoUrl url)
            => new CacheEntry(() => _inner.Resolve(url));

        private sealed class CacheEntry
        {
            private readonly Lazy<Components.IFileSource> _value;

            private bool _hasRefs;

            public CacheEntry(Func<Components.IFileSource> valueFactory)
            {
                _value = new Lazy<Components.IFileSource>(valueFactory);
            }

            public Components.IFileSource Value
            {
                get
                {
                    _hasRefs = true;
                    return _value.Value;
                }
            }

            public bool ResetRefs()
            {
                if (_hasRefs)
                {
                    _hasRefs = false;
                    return false;
                }

                return true;
            }
        }
    }
}

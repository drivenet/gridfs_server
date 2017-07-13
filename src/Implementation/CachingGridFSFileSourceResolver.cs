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
                const int CacheCleanIntervalSeconds = 300;
                time += Stopwatch.Frequency * CacheCleanIntervalSeconds;
                if (Interlocked.CompareExchange(ref _nextTime, time, nextTime) == nextTime)
                {
                    CleanCache(url);
                }
            }

            return _cache.GetOrAdd(url, _cacheFactory).GetValue();
        }

        private void CleanCache(MongoUrl key)
        {
            foreach (var pair in _cache)
            {
                if (pair.Key != key && pair.Value.Reset())
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

            private long _refs;

            public CacheEntry(Func<Components.IFileSource> valueFactory)
            {
                _value = new Lazy<Components.IFileSource>(valueFactory);
            }

            public Components.IFileSource GetValue()
            {
                Interlocked.Increment(ref _refs);
                return _value.Value;
            }

            public bool Reset()
                => Interlocked.Exchange(ref _refs, 0) == 0;
        }
    }
}

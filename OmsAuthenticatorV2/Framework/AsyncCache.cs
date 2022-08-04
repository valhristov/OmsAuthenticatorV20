using System.Collections.Concurrent;
using System.Diagnostics;

namespace OmsAuthenticator.Framework
{
    public class Cache<TKey, TValue> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new();
        private readonly TimeSpan _expiration;

        public Cache(TimeSpan expiration)
        {
            _expiration = expiration;
        }

        [DebuggerStepThrough]
        public TValue AddOrUpdate(TKey key, Func<TValue> factory) =>
            _cache
                .AddOrUpdate(key,
                    k => new CacheEntry(factory, _expiration),
                    (k, old) => ShouldReplaceCacheEntry(old) ? new CacheEntry(factory, _expiration) : old)
                .Value;

        protected virtual bool ShouldReplaceCacheEntry(CacheEntry entry) =>
            entry.Expired;

        public void Cleanup()
        {
            var expired = _cache.Where(x => x.Value.Expired).ToList();

            foreach (var item in expired)
            {
                _cache.TryRemove(item.Key, out _);
            }
        }

        public int Count => _cache.Count;

        protected class CacheEntry : Lazy<TValue>
        {
            private readonly DateTimeOffset _expires;

            public bool Expired => _expires <= DateTimeOffset.UtcNow;

            public CacheEntry(Func<TValue> factory, TimeSpan lifetime) : base(factory)
            {
                Debug.WriteLine("New cache entry created");
                _expires = DateTimeOffset.UtcNow.Add(lifetime);
            }
        }
    }

    public class AsyncResultCache<TKey, TValue> : Cache<TKey, Task<Result<TValue>>>
        where TKey : notnull
    {
        public AsyncResultCache(TimeSpan expiration) : base(expiration)
        { }

        protected override bool ShouldReplaceCacheEntry(CacheEntry entry) =>
            entry.Expired || (entry.IsValueCreated && entry.Value.IsCompleted && entry.Value.Result.IsFailure);
    }
}

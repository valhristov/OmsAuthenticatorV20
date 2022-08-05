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
        public TValue GetOrReplace(TKey key, Func<TValue> factory)
        {
            return _cache
                .AddOrUpdate(key,
                    k => CreateEntry(factory),
                    (k, existingEntry) => ShouldReplaceEntry(existingEntry) ? CreateEntry(factory) : existingEntry)
                .Value;

            CacheEntry CreateEntry(Func<TValue> factory) =>
                new CacheEntry(factory, DateTimeOffset.UtcNow.Add(_expiration));
        }

        protected virtual bool ShouldReplaceEntry(CacheEntry entry) =>
            entry.Expires <= DateTimeOffset.UtcNow;

        public void Cleanup()
        {
            var expired = _cache.Where(x => ShouldReplaceEntry(x.Value)).ToList();

            foreach (var item in expired)
            {
                _cache.TryRemove(item.Key, out _);
            }
        }

        public int Count => _cache.Count;

        protected class CacheEntry : Lazy<TValue>
        {
            public DateTimeOffset Expires { get; }

            public CacheEntry(Func<TValue> factory, DateTimeOffset expires) : base(factory)
            {
                Debug.WriteLine("New cache entry created");
                Expires = expires;
            }
        }
    }

    public class AsyncResultCache<TKey, TValue> : Cache<TKey, Task<Result<TValue>>>
        where TKey : notnull
    {
        public AsyncResultCache(TimeSpan expiration) : base(expiration)
        { }

        protected override bool ShouldReplaceEntry(CacheEntry entry) =>
            base.ShouldReplaceEntry(entry) ||
            (entry.IsValueCreated && entry.Value.IsCompleted && entry.Value.Result.IsFailure);
    }
}

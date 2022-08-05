using System.Collections.Concurrent;
using System.Diagnostics;

namespace OmsAuthenticator.Framework
{
    public class Cache<TKey, TValue>
        where TKey : notnull
    {
        protected readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new();
        private readonly TimeSpan _lifetime;
        private readonly Func<DateTimeOffset> _getSystemTime;

        public Cache(TimeSpan lifetime, Func<DateTimeOffset> getSystemTime)
        {
            _lifetime = lifetime;
            _getSystemTime = getSystemTime;
        }

        [DebuggerStepThrough]
        public TValue AddOrUpdate(TKey key, Func<TValue> factory)
        {
            return _cache
                .AddOrUpdate(key,
                    k => CreateEntry(factory),
                    (k, old) => ShouldReplaceCacheEntry(old) ? CreateEntry(factory) : old)
                .Value;

            CacheEntry CreateEntry(Func<TValue> factory) =>
                new CacheEntry(factory, _getSystemTime().Add(_lifetime));
        }

        protected virtual bool ShouldReplaceCacheEntry(CacheEntry entry) =>
            entry.ExpirationDate <= _getSystemTime();

        public void Cleanup()
        {
            var expired = _cache.Where(x => ShouldReplaceCacheEntry(x.Value)).ToList();

            foreach (var item in expired)
            {
                _cache.TryRemove(item.Key, out _);
            }
        }

        public int Count => _cache.Count;

        protected class CacheEntry : Lazy<TValue>
        {
            public DateTimeOffset ExpirationDate { get; }

            public CacheEntry(Func<TValue> factory, DateTimeOffset expirationDate) : base(factory)
            {
                Debug.WriteLine("New cache entry created");
                ExpirationDate = expirationDate;
            }
        }
    }

    public class AsyncResultCache<TKey, TValue> : Cache<TKey, Task<Result<TValue>>>
        where TKey : notnull
    {
        public AsyncResultCache(TimeSpan lifetime, Func<DateTimeOffset> getSystemTime) : base(lifetime, getSystemTime)
        { }

        protected override bool ShouldReplaceCacheEntry(CacheEntry entry) =>
            base.ShouldReplaceCacheEntry(entry) ||
            (entry.IsValueCreated && entry.Value.IsCompleted && entry.Value.Result.IsFailure);

        public Task<Result<TValue>> FindLongestExpirationItem(Func<TKey, bool> predicate)
        {
            var task = _cache
                .Where(kv => predicate(kv.Key)) // only entries that match the predicate
                .Select(kv => kv.Value)
                .Where(entry => !ShouldReplaceCacheEntry(entry))
                .OrderByDescending(entry => entry.ExpirationDate)
                .Select(entry => entry.Value)
                .FirstOrDefault();

            return task ?? Task.FromResult(Result.Success<TValue>(default!));
        }
    }
}

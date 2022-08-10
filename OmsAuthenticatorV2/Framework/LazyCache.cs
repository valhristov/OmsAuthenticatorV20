using System.Collections.Concurrent;
using System.Diagnostics;

namespace OmsAuthenticator.Framework
{
    public abstract class LazyCache<TKey, TValue>
        where TKey : notnull
    {
        protected readonly ConcurrentDictionary<TKey, Lazy<TValue>> _cache = new();
        protected Func<DateTimeOffset> UtcNow { get; }

        public LazyCache(Func<DateTimeOffset> getSystemTimeUtc)
        {
            UtcNow = getSystemTimeUtc;
        }

        [DebuggerStepThrough]
        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> valueFactory)
        {
            return _cache
                .AddOrUpdate(key,
                    k => new Lazy<TValue>(() => valueFactory(k)),
                    (k, old) => ShouldReplaceCacheEntry(old) ? new Lazy<TValue>(() => valueFactory(k)) : old)
                // This will invoke valueFactory if an entry with the specified key is missing or should be replaced
                .Value;
        }

        protected virtual bool ShouldReplaceCacheEntry(Lazy<TValue> entry) =>
            false;

        public void Cleanup()
        {
            var expired = _cache.Where(x => ShouldReplaceCacheEntry(x.Value)).ToList();
            foreach (var item in expired)
            {
                _cache.TryRemove(item.Key, out _);
            }
        }

        public int Count => _cache.Count;
    }
}

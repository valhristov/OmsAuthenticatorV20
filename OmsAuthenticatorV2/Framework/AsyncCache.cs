using System.Collections.Concurrent;
using System.Diagnostics;

namespace OmsAuthenticator.Framework
{
    public abstract class LazyCache<TKey, TValue>
        where TKey : notnull
    {
        protected readonly ConcurrentDictionary<TKey, Lazy<TValue>> _cache = new();
        private readonly Func<DateTimeOffset> _getSystemTimeUtc;

        public LazyCache(Func<DateTimeOffset> getSystemTimeUtc)
        {
            _getSystemTimeUtc = getSystemTimeUtc;
        }

        protected DateTimeOffset UtcNow() => _getSystemTimeUtc();

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

    public class AsyncTokenResultCache : LazyCache<TokenKey, Task<Result<Token>>>
    {
        public AsyncTokenResultCache(Func<DateTimeOffset> getSystemTime) : base(getSystemTime)
        { }

        protected override bool ShouldReplaceCacheEntry(Lazy<Task<Result<Token>>> entry) =>
            entry.IsValueCreated &&
            entry.Value.IsCompleted &&
            entry.Value.Result.Select(token => token.Expires < UtcNow(), errors => true);

        public Task<Result<Token>> FindEntry(Predicate<TokenKey> predicate)
        {
            return _cache
                .Where(kv => predicate(kv.Key)) // only entries that match the predicate
                .Select(kv => kv.Value)
                .Where(entry => !ShouldReplaceCacheEntry(entry))
                .Select(entry => entry.Value)
                .DefaultIfEmpty(Task.FromResult(Result.Failure<Token>("Token does not exist")))
                .First();
        }
    }
}

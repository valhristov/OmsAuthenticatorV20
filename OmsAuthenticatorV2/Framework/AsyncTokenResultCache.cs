namespace OmsAuthenticator.Framework
{
    public class AsyncTokenResultCache : LazyCache<TokenKey, Task<Result<Token>>>
    {
        public AsyncTokenResultCache(Func<DateTimeOffset> getSystemTime) : base(getSystemTime)
        { }

        protected override bool ShouldReplaceCacheEntry(Lazy<Task<Result<Token>>> entry) =>
            entry.IsValueCreated &&
            entry.Value.IsCompleted && // only completed tasks
            entry.Value.Result.Select(
                token => token.Expires < UtcNow(), // replace expired
                errors => true); // replace failed

        public Task<Result<Token>> FindEntry(Predicate<TokenKey> predicate) =>
            _cache
                .Where(kv => predicate(kv.Key)) // only entries that match the predicate
                .Select(kv => kv.Value)
                .Where(entry => !ShouldReplaceCacheEntry(entry))
                .Select(entry => entry.Value)
                .DefaultIfEmpty(Task.FromResult(Result.Failure<Token>("Token does not exist")))
                .First();
    }
}

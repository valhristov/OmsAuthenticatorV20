using System.Collections.Concurrent;
using System.Diagnostics;

namespace OmsAuthenticator.Framework
{
    public class TokenCache 
    {
        private readonly ISystemTime _systemTime;

        public TokenCache(ISystemTime systemTime)
        {
            _systemTime = systemTime;
        }

        protected readonly ConcurrentDictionary<TokenKey, Lazy<Task<Result<Token>>>> _cachedItems = new();
        
        public int Count => _cachedItems.Count;

        [DebuggerStepThrough]
        public Task<Result<Token>> AddOrUpdate(TokenKey key, Func<TokenKey, Task<Result<Token>>> valueFactory)
        {
            return _cachedItems
                .AddOrUpdate(key,
                    k => GetNewToken(k),
                    (k, old) => IsExpired(old) ? GetNewToken(k) : old)
                // This will invoke valueFactory if the entry with the specified key was missing or should be replaced
                .Value;

            Lazy<Task<Result<Token>>> GetNewToken(TokenKey k) =>
                new Lazy<Task<Result<Token>>>(() => valueFactory(k));
        }

        /// <summary>
        /// Searches for cache entries by token key properties. This method is used when
        /// finding a token without request id.
        /// </summary>
        public Task<Result<Token>> FindEntry(Predicate<TokenKey> predicate) =>
            _cachedItems
                .Where(kv => predicate(kv.Key)) // only entries with key that matches the predicate
                .Select(kv => kv.Value)
                .Where(entry => !IsExpired(entry))
                .Select(entry => entry.Value)
                .DefaultIfEmpty(Task.FromResult(Result.Failure<Token>("Token does not exist")))
                .First();

        public void Cleanup() =>
            _cachedItems
                .Where(x => IsExpired(x.Value))
                .ToList() // copy, so that we can remove them
                .ForEach(item => _cachedItems.TryRemove(item.Key, out _));

        private bool IsExpired(Lazy<Task<Result<Token>>> entry) =>
            entry.IsValueCreated &&
            entry.Value.IsCompleted && // only completed tasks
            entry.Value.Result.Select(
                token => token.Expires < _systemTime.UtcNow, // replace expired
                errors => true); // replace failed
    }
}

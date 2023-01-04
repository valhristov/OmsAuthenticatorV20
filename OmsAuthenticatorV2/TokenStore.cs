using OmsAuthenticator.ApiAdapters;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator;

public class TokenStore
{
    private readonly TokenCache _cache;

    public TokenStore(TokenCache cache)
    {
        _cache = cache;
    }

    public async Task<Result<Token>> GetOrAddOmsTokenAsync(TokenKey.Oms tokenKey, Func<TokenKey.Oms, Task<Result<Token>>> getTokenAsync)
    {
        return await GetToken(tokenKey,
            FindCompatibleToken, // when requestId is null
            getTokenAsync);

        bool FindCompatibleToken(TokenKey key) =>
            key is TokenKey.Oms omsKey && omsKey.OmsId == tokenKey.OmsId && omsKey.ConnectionId == tokenKey.ConnectionId;
    }

    /// <summary>
    /// For API V1. The new API will automatically request new token when a compatible
    /// token is not found in the cache. The old API just returns not found.
    /// </summary>
    public async Task<Result<Token>> TryGetOmsTokenAsync(string omsId, string applicationId)
    {
        return await _cache.FindEntry(FindCompatibleToken);

        bool FindCompatibleToken(TokenKey key) =>
            key is TokenKey.Oms omsKey && omsKey.OmsId == omsId && omsKey.ApplicationId == applicationId;
    }

    public async Task<Result<Token>> GetTrueTokenAsync(TokenKey.TrueApi tokenKey, Func<TokenKey.TrueApi, Task<Result<Token>>> getTokenAsync)
    {
        return await GetToken(tokenKey,
            FindCompatibleToken, // when requestId is null
            getTokenAsync);

        bool FindCompatibleToken(TokenKey key) =>
            key is TokenKey.TrueApi;
    }

    private async Task<Result<Token>> GetToken<TKey>(TKey key, Predicate<TokenKey> findToken, Func<TKey, Task<Result<Token>>> getTokenAsync) where TKey : TokenKey
    {
        return key.RequestId is null
            ? await FindOrGetTokenFromCacheAsync(key with { RequestId = Guid.NewGuid().ToString() })
            : await GetTokenFromCacheAsync(key);

        async Task<Result<Token>> GetTokenFromCacheAsync(TKey tokenKey) =>
            await _cache.AddOrUpdate(tokenKey, _ => getTokenAsync(tokenKey));

        async Task<Result<Token>> FindOrGetTokenFromCacheAsync(TKey tokenKey)
        {
            var tokenResult = await _cache.FindEntry(findToken);

            tokenResult = await tokenResult.Select(
                token => Task.FromResult(Result.Success(token)),
                async _ => await GetTokenFromCacheAsync(tokenKey)); // We could not find existing token, get a new one

            return tokenResult;
        }
    }
}
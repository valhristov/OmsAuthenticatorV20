using OmsAuthenticator.ApiAdapters;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator;

public class TokenProvider
{
    private readonly TokenCache _cache;
    private readonly IOmsTokenAdapter _omsTokenAdapter;

    public TokenProvider(TokenCache cache, IOmsTokenAdapter omsTokenAdapter)
    {
        _cache = cache;
        _omsTokenAdapter = omsTokenAdapter;
    }

    public async Task<Result<Token>> GetOmsTokenAsync(TokenKey.Oms tokenKey)
    {
        return await GetToken(tokenKey,
            FindCompatibleToken, // when requestId is null
            _omsTokenAdapter.GetOmsTokenAsync);

        bool FindCompatibleToken(TokenKey key) =>
            key is TokenKey.Oms omsKey && omsKey.OmsId == tokenKey.OmsId && omsKey.ConnectionId == tokenKey.ConnectionId;
    }

    public async Task<Result<Token>> GetTrueTokenAsync(TokenKey.TrueApi tokenKey)
    {
        return await GetToken(tokenKey,
            FindCompatibleToken, // when requestId is null
            _omsTokenAdapter.GetTrueTokenAsync);

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
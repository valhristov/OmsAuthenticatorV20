using Microsoft.AspNetCore.Mvc;
using OmsAuthenticator.ApiAdapters;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Api.V2;

public class TokenControllerV2
{
    private readonly TokenCache _cache;
    private readonly IOmsTokenAdapter _omsTokenAdapter;

    public TokenControllerV2(TokenCache cache, IOmsTokenAdapter omsTokenAdapter)
    {
        _cache = cache;
        _omsTokenAdapter = omsTokenAdapter;
    }
    
    public async Task<IResult> GetOmsTokenAsync(
        [FromQuery(Name="omsid")]string? omsId,
        [FromQuery(Name = "connectionid")]string? connectionId,
        [FromQuery(Name = "requestid")]string? requestId)
    {
        if (omsId == null)
        {
            return Results.BadRequest(new TokenResponse(new[] { $"Query string parameter '{nameof(omsId)}' is required." }));
        }

        if (connectionId == null)
        {
            return Results.BadRequest(new TokenResponse(new[] { $"Query string parameter '{nameof(connectionId)}' is required." }));
        }

        return await GetToken(new TokenKey.Oms(omsId, connectionId, requestId),
            FindCompatibleToken, // when requestId is null
            _omsTokenAdapter.GetOmsTokenAsync);

        bool FindCompatibleToken(TokenKey key) =>
            key is TokenKey.Oms omsKey && omsKey.OmsId == omsId && omsKey.ConnectionId == connectionId;
    }

    public async Task<IResult> GetTrueTokenAsync([FromQuery(Name = "requestid")] string? requestId)
    {
        return await GetToken(new TokenKey.TrueApi(requestId),
            FindCompatibleToken, // when requestId is null
            _omsTokenAdapter.GetTrueTokenAsync);

        bool FindCompatibleToken(TokenKey key) =>
            key is TokenKey.TrueApi;
    }

    private async Task<IResult> GetToken<TKey>(TKey key, Predicate<TokenKey> findToken, Func<TKey, Task<Result<Token>>> getTokenAsync) where TKey : TokenKey
    {
        var tokenResult = key.RequestId is null
            ? await FindOrGetTokenFromCacheAsync(key with { RequestId = Guid.NewGuid().ToString() })
            : await GetTokenFromCacheAsync(key);

        return tokenResult.Select(
            token => Results.Ok(new TokenResponse(token.Value, token.RequestId, token.Expires)),
            errors => Results.UnprocessableEntity(new TokenResponse(errors)));

        async Task<Result<Token>> GetTokenFromCacheAsync(TKey tokenKey) =>
            await _cache.AddOrUpdate(tokenKey, _ => getTokenAsync(tokenKey));

        async Task<Result<Token>> FindOrGetTokenFromCacheAsync(TKey tokenKey)
        {
            tokenResult = await _cache.FindEntry(findToken);

            tokenResult = await tokenResult.Select(
                token => Task.FromResult(Result.Success(token)),
                async _ => await GetTokenFromCacheAsync(tokenKey)); // We could not find existing token, get a new one

            return tokenResult;
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using OmsAuthenticator.ApiAdapters.GISMT.V3;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Api.V2;

public class TokenControllerV2
{
    private readonly AsyncTokenResultCache _cache;
    private readonly GisAdapterV3 _omsTokenAdapter;

    public TokenControllerV2(AsyncTokenResultCache cache, GisAdapterV3 omsTokenAdapter)
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

        var tokenResult = requestId == null
            ? await FindExistingOrGetTokenAsync(new TokenKey(omsId, connectionId, Guid.NewGuid().ToString()))
            : await GetTokenAsync(new TokenKey(omsId, connectionId, requestId));

        return tokenResult.Select(
            token => Results.Ok(new TokenResponse(token.Value, token.RequestId, token.Expires)),
            errors => Results.UnprocessableEntity(new TokenResponse(errors)));

        async Task<Result<Token>> FindExistingOrGetTokenAsync(TokenKey tokenKey)
        {
            tokenResult = await _cache.FindEntry(key => key.OmsId == tokenKey.OmsId && key.ConnectionId == tokenKey.ConnectionId);

            tokenResult = await tokenResult.Select(
                token => Task.FromResult(Result.Success(token)),
                async _ => await GetTokenAsync(tokenKey)); // We could not find existing token, get a new one
            return tokenResult;
        }

        async Task<Result<Token>> GetTokenAsync(TokenKey tokenKey) =>
            await _cache.AddOrUpdate(tokenKey, _omsTokenAdapter.GetOmsTokenAsync);
    }
}
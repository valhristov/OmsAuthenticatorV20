using Microsoft.AspNetCore.Mvc;
using OmsAuthenticator.ApiAdapters;

namespace OmsAuthenticator.Api.V2;

public class TrueTokenControllerV2
{
    private readonly TokenStore _tokenStore;
    private readonly ITrueTokenAdapter _tokenAdapter;

    public TrueTokenControllerV2(TokenStore tokenStore, ITrueTokenAdapter tokenAdapter)
    {
        _tokenStore = tokenStore;
        _tokenAdapter = tokenAdapter;
    }

    public async Task<IResult> GetTokenAsync([FromQuery(Name = "requestid")] string? requestId)
    {
        var tokenResult = await _tokenStore.GetTrueTokenAsync(new TokenKey.TrueApi(requestId), _tokenAdapter.GetTrueTokenAsync);

        return tokenResult.Select(
            token => Results.Ok(new TokenResponseV2(token.Value, token.RequestId, token.Expires)),
            errors => Results.UnprocessableEntity(new TokenResponseV2(errors)));
    }
}

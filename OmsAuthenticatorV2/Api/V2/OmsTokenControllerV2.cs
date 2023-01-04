using Microsoft.AspNetCore.Mvc;
using OmsAuthenticator.ApiAdapters;

namespace OmsAuthenticator.Api.V2;

public class OmsTokenControllerV2
{
    private readonly TokenStore _tokenStore;
    private readonly IOmsTokenAdapter _tokenAdapter;

    public OmsTokenControllerV2(TokenStore tokenStore, IOmsTokenAdapter tokenAdapter)
    {
        _tokenStore = tokenStore;
        _tokenAdapter = tokenAdapter;
    }
    
    public async Task<IResult> GetTokenAsync(
        [FromQuery(Name="omsid")]string? omsId,
        [FromQuery(Name = "connectionid")]string? connectionId,
        [FromQuery(Name = "requestid")]string? requestId)
    {
        if (omsId == null)
        {
            return Results.BadRequest(new TokenResponseV2(new[] { $"Query string parameter '{nameof(omsId)}' is required." }));
        }

        if (connectionId == null)
        {
            return Results.BadRequest(new TokenResponseV2(new[] { $"Query string parameter '{nameof(connectionId)}' is required." }));
        }

        var tokenResult = await _tokenStore.GetOrAddOmsTokenAsync(new TokenKey.Oms(string.Empty, omsId, connectionId, requestId), _tokenAdapter.GetOmsTokenAsync);

        return tokenResult.Select(
            token => Results.Ok(new TokenResponseV2(token.Value, token.RequestId, token.Expires)),
            errors => Results.UnprocessableEntity(new TokenResponseV2(errors)));
    }
}

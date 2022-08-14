using Microsoft.AspNetCore.Mvc;

namespace OmsAuthenticator.Api.V2;

public class TokenControllerV2
{
    private readonly TokenProvider _tokenProvider;

    public TokenControllerV2(TokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }
    
    public async Task<IResult> GetOmsTokenAsync(
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

        var tokenResult = await _tokenProvider.GetOmsTokenAsync(new TokenKey.Oms(omsId, connectionId, requestId));

        return tokenResult.Select(
            token => Results.Ok(new TokenResponseV2(token.Value, token.RequestId, token.Expires)),
            errors => Results.UnprocessableEntity(new TokenResponseV2(errors)));
    }

    public async Task<IResult> GetTrueTokenAsync([FromQuery(Name = "requestid")] string? requestId)
    {
        var tokenResult = await _tokenProvider.GetTrueTokenAsync(new TokenKey.TrueApi(requestId));

        return tokenResult.Select(
            token => Results.Ok(new TokenResponseV2(token.Value, token.RequestId, token.Expires)),
            errors => Results.UnprocessableEntity(new TokenResponseV2(errors)));
    }
}

using Microsoft.AspNetCore.Mvc;

namespace OmsAuthenticator.Api.V1
{
    public class TokenControllerV1
    {
        private readonly TokenProvider _tokenProvider;

        public TokenControllerV1(TokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider;
        }

        public async Task<IResult> GetAsync([FromQuery]string? omsId, [FromQuery]string? registrationKey)
        {
            if (omsId == null)
            {
                return Results.BadRequest(new TokenResponse(new[] { $"{nameof(omsId)} query string parameter is required." }));
            }
            if (registrationKey == null)
            {
                return Results.BadRequest(new TokenResponse(new[] { $"{nameof(registrationKey)} query string parameter is required." }));
            }

            return Results.UnprocessableEntity(new TokenResponse(new[] { "Not implemented" }));
        }

        public async Task<IResult> PostAsync([FromBody]TokenRequest? request)
        {
            if (request == null)
            {
                return Results.BadRequest(new TokenResponse(new[] { $"Invalid request." }));
            }
            if (request.OmsConnection == null)
            {
                return Results.BadRequest(new TokenResponse(new[] { $"omsConnection body parameter is required." }));
            }
            if (request.OmsId == null)
            {
                return Results.BadRequest(new TokenResponse(new[] { $"omsId body parameter is required." }));
            }

            var tokenResult = await _tokenProvider.GetOmsTokenAsync(new TokenKey.Oms(request.OmsId, request.OmsConnection, request.RequestId));

            return tokenResult.Select(
                token => Results.Ok(new TokenResponse(token.Value, token.RequestId, token.Expires)),
                errors => Results.UnprocessableEntity(new TokenResponse(errors)));
        }
    }
}

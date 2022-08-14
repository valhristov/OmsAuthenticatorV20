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
                return Results.BadRequest(new TokenResponseV1(new[] { $"{nameof(omsId)} query string parameter is required." }));
            }
            if (registrationKey == null)
            {
                return Results.BadRequest(new TokenResponseV1(new[] { $"{nameof(registrationKey)} query string parameter is required." }));
            }

            var tokenResult = await _tokenProvider.TryGetOmsTokenAsync(omsId, registrationKey);

            return tokenResult.Select(
                token => Results.Ok(new TokenResponseV1(token.Value, token.RequestId, token.Expires)),
                errors => Results.NotFound(new TokenResponseV1(errors)));
        }

        public async Task<IResult> PostAsync([FromBody]TokenRequestV1? request)
        {
            if (request == null)
            {
                return Results.BadRequest(new TokenResponseV1(new[] { $"Invalid request." }));
            }
            if (request.RegistrationKey == null)
            {
                return Results.BadRequest(new TokenResponseV1(new[] { $"registrationKey body parameter is required." }));
            }
            if (request.OmsConnection == null)
            {
                return Results.BadRequest(new TokenResponseV1(new[] { $"omsConnection body parameter is required." }));
            }
            if (request.OmsId == null)
            {
                return Results.BadRequest(new TokenResponseV1(new[] { $"omsId body parameter is required." }));
            }

            var tokenResult = await _tokenProvider.GetOrAddOmsTokenAsync(new TokenKey.Oms(request.RegistrationKey, request.OmsId, request.OmsConnection, request.RequestId));

            return tokenResult.Select(
                token => Results.Ok(new TokenResponseV1(token.Value, token.RequestId, token.Expires)),
                errors => Results.UnprocessableEntity(new TokenResponseV1(errors)));
        }
    }
}

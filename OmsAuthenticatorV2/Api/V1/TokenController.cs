using Microsoft.AspNetCore.Mvc;

namespace OmsAuthenticator.Api.V1
{
    public class TokenController
    {
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
            return Results.BadRequest(new TokenResponse(new[] { $"{nameof(request)} query string parameter is required." }));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using OmsAuthenticator.ApiAdapters;

namespace OmsAuthenticator.Api.V1
{
    public class SignatureControllerV1
    {
        private readonly IOmsTokenAdapter _adapter;

        public SignatureControllerV1(IOmsTokenAdapter adapter)
        {
            _adapter = adapter;
        }

        public async Task<IResult> PostAsync([FromBody] SignatureRequest? request)
        {
            if (request == null)
            {
                return Results.BadRequest(new TokenResponse(new[] { $"Invalid request." }));
            }
            if (request.PayloadBase64 == null)
            {
                return Results.BadRequest(new TokenResponse(new[] { $"payloadBase64 body parameter is required." }));
            }

            var result = await _adapter.SignAsync(request.PayloadBase64);

            return result.Select(
                signed => Results.Ok(new SignatureResponse(signed)),
                errors => Results.UnprocessableEntity(new SignatureResponse(errors)));
        }
    }
}

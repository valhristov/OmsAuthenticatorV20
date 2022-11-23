using Microsoft.AspNetCore.Mvc;
using OmsAuthenticator.ApiAdapters;

namespace OmsAuthenticator.Api.V1
{
    public class SignatureControllerV1
    {
        private readonly ITokenAdapter _adapter;

        public SignatureControllerV1(ITokenAdapter adapter)
        {
            _adapter = adapter;
        }

        public async Task<IResult> PostAsync([FromBody] SignatureRequestV1? request)
        {
            if (request == null)
            {
                return Results.BadRequest(new SignatureResponseV1(new[] { $"Invalid request." }));
            }
            if (request.PayloadBase64 == null)
            {
                return Results.BadRequest(new SignatureResponseV1(new[] { $"payloadBase64 body parameter is required." }));
            }

            var result = await _adapter.SignAsync(request.PayloadBase64);

            return result.Select(
                signed => Results.Ok(new SignatureResponseV1(signed)),
                errors => Results.UnprocessableEntity(new SignatureResponseV1(errors)));
        }
    }
}

﻿using Microsoft.AspNetCore.Mvc;
using OmsAuthenticator.ApiAdapters;

namespace OmsAuthenticator.Api.V2
{
    public class SignatureControllerV2
    {
        private readonly IOmsTokenAdapter _adapter;

        public SignatureControllerV2(IOmsTokenAdapter adapter)
        {
            _adapter = adapter;
        }

        public async Task<IResult> PostAsync([FromBody] SignatureRequestV2? request)
        {
            if (request == null)
            {
                return Results.BadRequest(new TokenResponseV2(new[] { $"Invalid request." }));
            }
            if (request.PayloadBase64 == null)
            {
                return Results.BadRequest(new TokenResponseV2(new[] { $"payloadBase64 body parameter is required." }));
            }

            var result = await _adapter.SignAsync(request.PayloadBase64);

            return result.Select(
                signed => Results.Ok(new SignatureResponseV2(signed)),
                errors => Results.UnprocessableEntity(new SignatureResponseV2(errors)));
        }
    }
}

﻿using Microsoft.AspNetCore.Mvc;
using OmsAuthenticator.ApiAdapters;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Api.V1
{
    public class TokenController
    {
        private readonly AsyncTokenResultCache _cache;
        private readonly IOmsTokenAdapter _tokenAdapter;

        public TokenController(AsyncTokenResultCache cache, IOmsTokenAdapter tokenAdapter)
        {
            _cache = cache;
            _tokenAdapter = tokenAdapter;
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

            var tokenKey = new TokenKey(request.OmsId, request.OmsConnection, request.RequestId ?? Guid.NewGuid().ToString());

            var tokenResult = await _cache.AddOrUpdate(tokenKey, async key => await _tokenAdapter.GetOmsTokenAsync(key));

            return tokenResult.Select(
                token => Results.Ok(new TokenResponse(token.Value, tokenKey.RequestId, token.Expires)),
                errors => Results.UnprocessableEntity(new TokenResponse(errors)));
        }
    }
}
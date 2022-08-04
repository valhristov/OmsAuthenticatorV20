using System.Net;
using System.Text.Json;
using FluentAssertions;
using OmsAuthenticator.Api.V1;

namespace OmsAuthenticator.Tests.Api.V1
{
    public static class ResponseShould
    {
        public static void BeBadRequest(HttpResponseMessage response, string responseContent)
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent)!;

            tokenResponse.Should().NotBeNull();
            tokenResponse.Token.Should().BeNull();
            tokenResponse.Errors.Should().NotBeEmpty();
        }
    }
}
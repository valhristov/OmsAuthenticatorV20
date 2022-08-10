using System.Net;
using System.Text.Json;
using FluentAssertions;
using OmsAuthenticator.Api.V2;

namespace OmsAuthenticator.Tests.Api.V2
{
    public static class ResponseShould
    {
        public static async Task BeBadRequest(HttpResponseMessage response)
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(await response.Content.ReadAsStringAsync())!;

            tokenResponse.Should().NotBeNull();
            tokenResponse.Token.Should().BeNull();
            tokenResponse.Errors.Should().NotBeEmpty();
        }

        public static async Task BeOkResult(HttpResponseMessage response, string expectedToken, string? expectedRequestId = null)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(await response.Content.ReadAsStringAsync())!;

            tokenResponse.Should().NotBeNull();
            tokenResponse.Token.Should().Be(expectedToken);
            if (expectedRequestId != null)
            {
                tokenResponse.RequestId.Should().Be(expectedRequestId);
            }
            tokenResponse.Errors.Should().BeNull();
        }
    }
}
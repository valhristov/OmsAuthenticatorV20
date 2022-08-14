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

            var tokenResponse = JsonSerializer.Deserialize<TokenResponseV2>(await response.Content.ReadAsStringAsync())!;

            tokenResponse.Should().NotBeNull();
            tokenResponse.Token.Should().BeNull();
            tokenResponse.Errors.Should().NotBeEmpty();
        }

        public static async Task BeOk(HttpResponseMessage response, string expectedToken, string? expectedRequestId = null)
        {
            var tokenResponse = JsonSerializer.Deserialize<TokenResponseV2>(await response.Content.ReadAsStringAsync())!;

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            tokenResponse.Should().NotBeNull();
            tokenResponse.Token.Should().Be(expectedToken);
            if (expectedRequestId != null)
            {
                tokenResponse.RequestId.Should().Be(expectedRequestId);
            }
            tokenResponse.Errors.Should().BeNull();
        }

        public static async Task BeUnprocessableEntity(HttpResponseMessage response, string expectedError)
        {
            var tokenResponse = JsonSerializer.Deserialize<TokenResponseV2>(await response.Content.ReadAsStringAsync())!;

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            tokenResponse.Should().NotBeNull();
            tokenResponse.Token.Should().BeNull();
            tokenResponse.Errors.Should().NotBeNullOrEmpty();
            tokenResponse.Errors.Should().Contain(expectedError);
        }
    }
}
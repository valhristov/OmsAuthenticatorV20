using System.Net;
using System.Text.Json;
using FluentAssertions;
using RichardSzalay.MockHttp;

namespace OmsAuthenticator.Tests
{
    public class TrueApiTokenApiHelper
    {
        private readonly MockHttpMessageHandler _httpClientMock;

        public TrueApiTokenApiHelper(MockHttpMessageHandler httpClientMock)
        {
            _httpClientMock = httpClientMock;
        }

        public void ExpectGetTokenSequence(string token)
        {
            var data = ExpectGetCertKeyRequest();
            ExpectGetTokenRequest(data, HttpStatusCode.OK, JsonSerializer.Serialize(new { token = token }));
        }

        public void ExpectGetTokenSequence(HttpStatusCode statusCode, string content)
        {
            var data = ExpectGetCertKeyRequest();
            ExpectGetTokenRequest(data, statusCode, content);
        }

        public string ExpectGetCertKeyRequest()
        {
            var data = Guid.NewGuid().ToString().Replace("-", "");
            ExpectGetCertKeyRequest(HttpStatusCode.OK,
                JsonSerializer.Serialize(new { uuid = Guid.NewGuid().ToString(), data = data }));
            return data;
        }

        public void ExpectGetCertKeyRequest(HttpStatusCode statusCode, string responseContent) =>
            _httpClientMock
                .Expect(HttpMethod.Get, "/api/v3/auth/cert/key")
                .Respond(statusCode, new StringContent(responseContent));

        public void VerifyNoOutstandingExpectation() =>
            _httpClientMock.VerifyNoOutstandingExpectation();

        private void ExpectGetTokenRequest(string data, HttpStatusCode statusCode, string responseContent)
        {
            data.Should().NotStartWith("signed:", because: "'signed:' will be added automatically.");
            _httpClientMock
                .Expect(HttpMethod.Post, $"/api/v3/true-api/auth/simpleSignIn")
                .WithPartialContent("signed:" + data)
                .Respond(statusCode, new StringContent(responseContent));
        }
    }
}
using System.Net;
using System.Text.Json;
using RichardSzalay.MockHttp;

namespace OmsAuthenticator.Tests
{
    public class GisMtHelper
    {
        private readonly MockHttpMessageHandler _httpClientMock = new MockHttpMessageHandler();

        public HttpClient GetHttpClient()
        {
            var client = _httpClientMock.ToHttpClient();
            client.BaseAddress = new Uri("http://test.com");
            return client;
        }

        public void SetupGetTokenRequest(string omsConnection, string data, string token) =>
            SetupGetTokenRequest(omsConnection, data, HttpStatusCode.OK, JsonSerializer.Serialize(new { token = token}));

        public void SetupGetTokenRequest(string omsConnection, string data, HttpStatusCode statusCode, string responseContent) =>
            _httpClientMock
                .Expect(HttpMethod.Post, $"/api/v3/auth/cert/{omsConnection}")
                .WithPartialContent(data)
                .Respond(statusCode, new StringContent(responseContent));

        public void SetupGetCertKeyRequest(string data) =>
            SetupGetCertKeyRequest(HttpStatusCode.OK,
                JsonSerializer.Serialize(new { uuid = Guid.NewGuid().ToString(), data = data }));
        
        public void SetupGetCertKeyRequest(HttpStatusCode statusCode, string responseContent) =>
            _httpClientMock
                .Expect(HttpMethod.Get, "/api/v3/auth/cert/key")
                .Respond(statusCode, new StringContent(responseContent));

        internal void VerifyNoOutstandingExpectation() =>
            _httpClientMock.VerifyNoOutstandingExpectation();
    }
}
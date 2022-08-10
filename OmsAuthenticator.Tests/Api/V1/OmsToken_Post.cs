using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using OmsAuthenticator.Api.V1;
using RichardSzalay.MockHttp;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace OmsAuthenticator.Tests.Api.V1
{
    [TestClass]
    public class OmsToken_Post
    {
        private readonly MockHttpMessageHandler _httpClientMock;
        private readonly OmsAuthenticatorApp _app;
        private readonly HttpClient _client;

        public OmsToken_Post()
        {
            _httpClientMock = new MockHttpMessageHandler();

            _app = new OmsAuthenticatorApp(() => _httpClientMock.ToHttpClient());
            _client = _app.CreateClient();
        }

        private async Task<HttpResponseMessage> PostAsync(object request) => 
            await _client.PostAsync("/oms/token", new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));

        private void SetupGetTokenRequest(string omsConnection, string data, string token)
        {
            _httpClientMock
                .Expect(HttpMethod.Post, $"/api/v3/auth/cert/{omsConnection}")
                .WithPartialContent(data)
                .Respond(HttpStatusCode.OK, new StringContent(JsonSerializer.Serialize(new { token = token })));
        }

        private void SetupGetCertKeyRequest(string data)
        {
            _httpClientMock
                .Expect(HttpMethod.Get, "/api/v3/auth/cert/key")
                .Respond(HttpStatusCode.OK, new StringContent(JsonSerializer.Serialize(new { uuid = NewGuid(), data = data })));
        }

        [DataTestMethod]
        [DataRow("/oms/token", "")]
        [DataRow("/oms/token", "{}")]
        [DataRow("/oms/token", @"{ ""omsId"": ""best"" }")]
        [DataRow("/oms/token", @"{ ""registrationKey"": ""best"" }")]
        [DataRow("/oms/token", @"{ ""requestId"": ""best"" }")]
        [DataRow("/oms/token", @"{ ""omsId"": ""best"", ""requestId"": ""test"" }")]
        [DataRow("/oms/token?omsId=xxx", "{}")]
        [DataRow("/oms/token?registrationKey=xxx", "{}")]
        public async Task Invalid_Request(string url, string body)
        {
            // Arrange

            // Act
            using var response = await _client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));

            // Assert
            await ResponseShould.BeBadRequest(response);
        }

        [TestMethod]
        public async Task No_Token_In_Cache()
        {
            var omsConnection = NewGuid();

            SetupGetCertKeyRequest("the data");
            SetupGetTokenRequest(omsConnection, "the data", "the token 1");

            var result = await PostAsync(new { omsConnection = omsConnection, omsId = NewGuid(), requestId = NewGuid(), });

            await ResponseShould.BeOkResult(result, "the token 1");

            _httpClientMock.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task Token_Is_Cached()
        {
            // The token key consists of omd id, registration key and request id

            var omsConnection = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();

            SetupGetCertKeyRequest("the data");
            SetupGetTokenRequest(omsConnection, "the data", "the token");

            var result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token");

            result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token");

            result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token");

            _httpClientMock.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task New_Token_Forced()
        {
            // The token key consists of omd id, registration key and request id

            var omsConnection = NewGuid();
            var omsId = NewGuid();

            SetupGetCertKeyRequest("the data 1");
            SetupGetTokenRequest(omsConnection, "the data 1", "the token 1");
            SetupGetCertKeyRequest("the data 2");
            SetupGetTokenRequest(omsConnection, "the data 2", "the token 2");
            SetupGetCertKeyRequest("the data 3");
            SetupGetTokenRequest(omsConnection, "the data 3", "the token 3");

            var result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = NewGuid(), });
            await ResponseShould.BeOkResult(result, "the token 1");

            result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = NewGuid(), });
            await ResponseShould.BeOkResult(result, "the token 2");

            result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = NewGuid(), });
            await ResponseShould.BeOkResult(result, "the token 3");

            _httpClientMock.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task Token_Expires_In_Cache()
        {
            // The token key consists of omd id, registration key and request id

            var omsConnection = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();

            SetupGetCertKeyRequest("the data 1");
            SetupGetTokenRequest(omsConnection, "the data 1", "the token 1");
            SetupGetCertKeyRequest("the data 2");
            SetupGetTokenRequest(omsConnection, "the data 2", "the token 2");
            SetupGetCertKeyRequest("the data 3");
            SetupGetTokenRequest(omsConnection, "the data 3", "the token 3");

            var result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token 1");

            _app.Wait(TimeSpan.FromHours(10)); // wait for the token to expire

            result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token 2");

            _app.Wait(TimeSpan.FromHours(10)); // wait for the token to expire

            result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token 3");

            _httpClientMock.VerifyNoOutstandingExpectation();
        }

        [DebuggerStepThrough]
        private string NewGuid() =>
            Guid.NewGuid().ToString();
    }
}
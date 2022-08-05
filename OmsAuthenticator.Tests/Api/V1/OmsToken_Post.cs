using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OmsAuthenticator.Api.V1;
using RichardSzalay.MockHttp;

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

        private async Task<HttpResponseMessage> PostAsync(TokenRequest request) => 
            await _client.PostAsync("/oms/token", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

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
        public async Task GetToken_Invalid_Post(string url, string body)
        {
            // Arrange

            // Act
            using var response = await _client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));

            // Assert
            await ResponseShould.BeBadRequest(response);
        }

        [TestMethod]
        public async Task First_Request_With_OmsConnection()
        {
            var omsConnection = NewGuid();

            SetupGetCertKeyRequest("the data");
            SetupGetTokenRequest(omsConnection, "the data", "the token 1");

            var result = await PostAsync(new TokenRequest { RegistrationKey = NewGuid(), OmsConnection = omsConnection, OmsId = NewGuid(), RequestId = NewGuid(), });

            await ResponseShould.BeOkResult(result, "the token 1");

            //_httpTestHelper.VerifyGetCertKeyRequest(Times.Once());
            //_httpTestHelper.VerifyGetTokenRequest(omsConnection, Times.Once());
        }

        [TestMethod]
        public async Task Subsequent_Requests_With_OmsConnection_Same_Key()
        {
            // The token key consists of omd id, registration key and request id

            var omsConnection = NewGuid();
            var registrationKey = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();

            SetupGetCertKeyRequest("the data");
            SetupGetTokenRequest(omsConnection, "the data", "the token");

            var result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsConnection = omsConnection, OmsId = omsId, RequestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token");

            result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsConnection = omsConnection, OmsId = omsId, RequestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token");

            result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsConnection = omsConnection, OmsId = omsId, RequestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token");

            //_httpTestHelper.VerifyGetCertKeyRequest(Times.Once());
            //_httpTestHelper.VerifyGetTokenRequest(omsConnection, Times.Once()); // we obtained only 1 token
        }

        [TestMethod]
        public async Task Subsequent_Requests_With_OmsConnection_Different_RequestId()
        {
            // The token key consists of omd id, registration key and request id

            var omsConnection = NewGuid();
            var registrationKey = NewGuid();
            var omsId = NewGuid();

            SetupGetCertKeyRequest("the data 1");
            SetupGetTokenRequest(omsConnection, "the data 1", "the token 1");
            SetupGetCertKeyRequest("the data 2");
            SetupGetTokenRequest(omsConnection, "the data 2", "the token 2");
            SetupGetCertKeyRequest("the data 3");
            SetupGetTokenRequest(omsConnection, "the data 3", "the token 3");

            var result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsConnection = omsConnection, OmsId = omsId, RequestId = NewGuid(), });
            await ResponseShould.BeOkResult(result, "the token 1");

            result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsConnection = omsConnection, OmsId = omsId, RequestId = NewGuid(), });
            await ResponseShould.BeOkResult(result, "the token 2");

            result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsConnection = omsConnection, OmsId = omsId, RequestId = NewGuid(), });
            await ResponseShould.BeOkResult(result, "the token 3");

            //_httpTestHelper.VerifyGetCertKeyRequest(Times.Exactly(3));
            //_httpTestHelper.VerifyGetTokenRequest(omsConnection, Times.Exactly(3)); // we obtained 3 tokens
        }

        [TestMethod]
        public async Task Subsequent_Requests_With_OmsConnection_Same_Key_After_Expiration()
        {
            // The token key consists of omd id, registration key and request id

            var omsConnection = NewGuid();
            var registrationKey = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();

            SetupGetCertKeyRequest("the data 1");
            SetupGetTokenRequest(omsConnection, "the data 1", "the token 1");
            SetupGetCertKeyRequest("the data 2");
            SetupGetTokenRequest(omsConnection, "the data 2", "the token 2");
            SetupGetCertKeyRequest("the data 3");
            SetupGetTokenRequest(omsConnection, "the data 3", "the token 3");

            var result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsConnection = omsConnection, OmsId = omsId, RequestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token 1");

            await Task.Delay(600); // wait for the token to expire

            result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsConnection = omsConnection, OmsId = omsId, RequestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token 2");

            await Task.Delay(600); // wait for the token to expire

            result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsConnection = omsConnection, OmsId = omsId, RequestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token 3");

            await Task.Delay(600); // wait for the token to expire

            //_httpTestHelper.VerifyGetCertKeyRequest(Times.Exactly(3));
            //_httpTestHelper.VerifyGetTokenRequest(omsConnection, Times.Exactly(3)); // we obtained only 1 token
        }

        [TestMethod]
        public async Task First_Request_Without_OmsConnection()
        {
            var omsConnection = NewGuid();

            //_httpTestHelper.SetupIntegrationGetConnectionRequest(omsConnection);
            //_httpTestHelper.SetupGetCertKeyRequest("the uuid", "the data");
            //_httpTestHelper.SetupGetTokenRequests(omsConnection, "the token");

            var result = await PostAsync(new TokenRequest { RegistrationKey = NewGuid(), OmsId = NewGuid(), RequestId = NewGuid(), });

            await ResponseShould.BeOkResult(result, "the token");

            //_httpTestHelper.VerifyIntegrationGetConnectionRequest(Times.Once());
            //_httpTestHelper.VerifyGetCertKeyRequest(Times.Once());
            //_httpTestHelper.VerifyGetTokenRequest(omsConnection, Times.Once());
        }

        [TestMethod]
        public async Task Subsequent_Requests_Without_OmsConnection_Same_Key()
        {
            // The token key consists of omd id, registration key and request id

            var omsConnection = NewGuid();
            var registrationKey = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();

            //_httpTestHelper.SetupIntegrationGetConnectionRequest(omsConnection);
            //_httpTestHelper.SetupGetCertKeyRequest("the uuid", "the data");
            //_httpTestHelper.SetupGetTokenRequests(omsConnection, "the token");

            var result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsId = omsId, RequestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token");

            result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsId = omsId, RequestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token");

            result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsId = omsId, RequestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token");

            //_httpTestHelper.VerifyIntegrationGetConnectionRequest(Times.Once());
            //_httpTestHelper.VerifyGetCertKeyRequest(Times.Once());
            //_httpTestHelper.VerifyGetTokenRequest(omsConnection, Times.Once()); // we obtained only 1 token
        }

        [TestMethod]
        public async Task Subsequent_Requests_Without_OmsConnection_Same_Key_After_Expiration()
        {
            // The token key consists of omd id, registration key and request id

            var omsConnection = NewGuid();
            var registrationKey = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();

            //_httpTestHelper.SetupIntegrationGetConnectionRequest(omsConnection);
            //_httpTestHelper.SetupGetCertKeyRequest("the uuid", "the data");
            //_httpTestHelper.SetupGetTokenRequests(omsConnection, "the token 1", "the token 2", "the token 3");

            var result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsId = omsId, RequestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token 1");

            await Task.Delay(600); // wait for the token to expire

            result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsId = omsId, RequestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token 2");

            await Task.Delay(600); // wait for the token to expire

            result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsId = omsId, RequestId = requestId, });
            await ResponseShould.BeOkResult(result, "the token 3");

            await Task.Delay(600); // wait for the token to expire

            //_httpTestHelper.VerifyIntegrationGetConnectionRequest(Times.Exactly(3));
            //_httpTestHelper.VerifyGetCertKeyRequest(Times.Exactly(3));
            //_httpTestHelper.VerifyGetTokenRequest(omsConnection, Times.Exactly(3)); // we obtained only 1 token
        }

        [TestMethod]
        public async Task Subsequent_Requests_Without_OmsConnection_Different_RequestId()
        {
            // The token key consists of omd id, registration key and request id

            var omsConnection = NewGuid();
            var registrationKey = NewGuid();
            var omsId = NewGuid();

            //_httpTestHelper.SetupIntegrationGetConnectionRequest(omsConnection);
            //_httpTestHelper.SetupGetCertKeyRequest("the uuid", "the data");
            //_httpTestHelper.SetupGetTokenRequests(omsConnection, "the token 1", "the token 2", "the token 3");

            var result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsId = omsId, RequestId = NewGuid(), });
            await ResponseShould.BeOkResult(result, "the token 1");

            result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsId = omsId, RequestId = NewGuid(), });
            await ResponseShould.BeOkResult(result, "the token 2");

            result = await PostAsync(new TokenRequest { RegistrationKey = registrationKey, OmsId = omsId, RequestId = NewGuid(), });
            await ResponseShould.BeOkResult(result, "the token 3");

            //_httpTestHelper.VerifyIntegrationGetConnectionRequest(Times.Exactly(3));
            //_httpTestHelper.VerifyGetCertKeyRequest(Times.Exactly(3));
            //_httpTestHelper.VerifyGetTokenRequest(omsConnection, Times.Exactly(3)); // we obtained 3 tokens
        }

        [DebuggerStepThrough]
        private string NewGuid() =>
            Guid.NewGuid().ToString();
    }
}
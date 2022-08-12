using System.Diagnostics;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OmsAuthenticator.Tests.Api.V2
{
    [TestClass]
    public class OmsToken_Get
    {
        private static readonly TimeSpan Expiration = TimeSpan.FromHours(10);

        private OmsAuthenticatorApp App { get; }
        private HttpClient Client { get; }

        public OmsToken_Get()
        {
            // Using "integration tests" for SignDataPath to short-cirquit the signer
            App = new OmsAuthenticatorApp($@"{{
  ""Authenticator"": {{
    ""SignDataPath"": "".\\SignData.exe"",
    ""TokenProviders"": {{
      ""key1"": {{
        ""Adapter"": ""gis-v3"",
        ""Certificate"": ""integrationtests"",
        ""Url"": ""https://demo.crpt.ru"",
        ""Expiration"": ""{Expiration}""
      }}
    }}
  }}
}}");
            Client = App.CreateClient();
        }

        [DebuggerStepThrough]
        private string NewGuid() => Guid.NewGuid().ToString();

        [DebuggerStepThrough]
        private string NewDataToSign() => Guid.NewGuid().ToString().Replace("-", "");

        [DebuggerStepThrough]
        private void WaitForTokenToExpire() => App.Wait(Expiration.Add(TimeSpan.FromSeconds(1)));

        [DataTestMethod]
        [DataRow("/api/v2/key1/oms/token")]
        [DataRow("/api/v2/key1/oms/token?omsid=best")]
        [DataRow("/api/v2/key1/oms/token?connectionid=best")]
        [DataRow("/api/v2/key1/oms/token?omsId=best&requestId=1")]
        [DataRow("/api/v2/key1/oms/token?connectionid=best&requestid=1")]
        public async Task Invalid_Request(string url)
        {
            // Arrange

            // Act
            using var response = await Client.GetAsync(url);

            // Assert
            await ResponseShould.BeBadRequest(response);
        }

        [DataTestMethod]
        [DataRow("&requestid=1")] // we get token with request id
        [DataRow("")] // we get token without request id
        public async Task No_Token_In_Cache(string requestIdParameter)
        {
            // In this scenario the cache is empty. Requesting token with
            // and without requestid parameter should return a new token.

            // Arrange
            var omsConnection = NewGuid();
            var dataToSign = NewDataToSign();

            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token");

            // Act
            var result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={NewGuid()}&connectionid={omsConnection}{requestIdParameter}");

            // Assert
            await ResponseShould.BeOk(result, "the token");

            App.GisMtApi.VerifyNoOutstandingExpectation();
        }

        [DataTestMethod]
        [DataRow("&requestid=1")] // we get token with request id
        [DataRow("")] // we get token without request id
        public async Task Expired_Token_In_Cache(string requestIdParameter)
        {
            // In this scenario the cache contains an expired token. Requesting token with
            // and without requestid parameter should return a new token.

            // Arrange
            var omsConnection = NewGuid();
            var dataToSign = NewDataToSign();

            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token");

            dataToSign = NewDataToSign();
            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token");

            // Cache a new token
            await Client.GetAsync($"/api/v2/key1/oms/token?omsid={NewGuid()}&connectionid={omsConnection}{requestIdParameter}");

            WaitForTokenToExpire();

            // Act
            var result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={NewGuid()}&connectionid={omsConnection}{requestIdParameter}");

            // Assert
            await ResponseShould.BeOk(result, "the token");

            App.GisMtApi.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task Token_Is_Cached()
        {
            // Here we ensure that only the first token request will obtain
            // a new token. Each next request should use the existing token
            // from the cache.

            // Arrange
            var omsConnection = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();
            var dataToSign = NewDataToSign();

            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token");

            // Act
            var result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            // Assert
            await ResponseShould.BeOk(result, "the token", requestId);

            // Act
            result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            // Assert
            await ResponseShould.BeOk(result, "the token", requestId);

            // Act
            result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            // Assert
            await ResponseShould.BeOk(result, "the token", requestId);

            App.GisMtApi.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task New_Token_Forced()
        {
            // Specifying a requestid that is different than the one of the
            // existing token in the cache will force obtaining a new token.

            // Arrange
            var omsConnection = NewGuid();
            var omsId = NewGuid();

            var dataToSign = NewDataToSign();
            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token 1");
            dataToSign = NewDataToSign();
            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token 2");
            dataToSign = NewDataToSign();
            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token 3");

            // Act
            var requestId = NewGuid();
            var result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            // Assert
            await ResponseShould.BeOk(result, "the token 1", requestId);

            // Act
            requestId = NewGuid();
            result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            // Assert
            await ResponseShould.BeOk(result, "the token 2", requestId);

            // Act
            requestId = NewGuid();
            result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            // Assert
            await ResponseShould.BeOk(result, "the token 3", requestId);

            App.GisMtApi.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task Existing_Token_Is_Returned()
        {
            // When requestid is not provided, we return the token that we have
            // in the cache.

            // Arrange
            var omsConnection = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();
            var dataToSign = NewDataToSign();

            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token");

            // Put a token in cache
            var result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            await ResponseShould.BeOk(result, "the token", requestId);

            // Act - no request id parameter, we should return existing token
            result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}");

            // Assert
            await ResponseShould.BeOk(result, "the token", requestId);

            App.GisMtApi.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task GetCertKey_Returns_Error()
        {
            // The errors from GIS MT API are relayed to our responses with nice error messages.

            // Arrange
            var omsId = NewGuid();
            var omsConnection = NewGuid();

            App.GisMtApi.SetupGetCertKeyRequest(HttpStatusCode.BadRequest, "some error");
            App.GisMtApi.SetupGetCertKeyRequest(HttpStatusCode.BadRequest, "some error");

            // Act
            var result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}");
            // Assert
            await ResponseShould.BeUnprocessableEntity(result, "[GET https://demo.crpt.ru/api/v3/auth/cert/key] Response was 400 with content 'some error'");

            // Act
            result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}");
            // Assert
            await ResponseShould.BeUnprocessableEntity(result, "[GET https://demo.crpt.ru/api/v3/auth/cert/key] Response was 400 with content 'some error'");

            // Both GIS MT API requests are invoked. This means that our second request invoked the API
            App.GisMtApi.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task GetToken_Returns_Error()
        {
            // The errors from GIS MT API are relayed to our responses with nice error messages.

            // Arrange
            var omsConnection = NewGuid();

            var dataToSign = NewDataToSign();
            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, HttpStatusCode.BadRequest, "some error");
            dataToSign = NewDataToSign();
            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, HttpStatusCode.BadRequest, "some error");

            // Act
            var result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={NewGuid()}&connectionid={omsConnection}");
            // Assert
            await ResponseShould.BeUnprocessableEntity(result, $"[POST https://demo.crpt.ru/api/v3/auth/cert/{omsConnection}] Response was 400 with content 'some error'");

            // Act
            result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={NewGuid()}&connectionid={omsConnection}");
            // Assert
            await ResponseShould.BeUnprocessableEntity(result, $"[POST https://demo.crpt.ru/api/v3/auth/cert/{omsConnection}] Response was 400 with content 'some error'");

            // All GIS MT API requests are invoked. This means that our second request invoked the API
            App.GisMtApi.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task SignData_Returns_Error()
        {
            // The errors from SignData are relayed to our responses with nice error messages.

            // Arrange
            // Using custom app because we have different certificate configuration for this test
            var app = new OmsAuthenticatorApp($@"{{
  ""Authenticator"": {{
    ""SignDataPath"": "".\\SignData.exe"",
    ""TokenProviders"": {{
      ""key1"": {{
        ""Adapter"": ""gis-v3"",
        ""Certificate"": ""123"",
        ""Url"": ""https://demo.crpt.ru"",
        ""Expiration"": ""{Expiration}""
      }}
    }}
  }}
}}");

            var omsConnection = NewGuid();
            var dataToSign = NewDataToSign();

            app.GisMtApi.SetupGetCertKeyRequest(dataToSign);

            // Act
            var result = await app.CreateClient().GetAsync($"/api/v2/key1/oms/token?omsid={NewGuid()}&connectionid={omsConnection}");

            // Assert
            await ResponseShould.BeUnprocessableEntity(result, "[SignData.exe] Cannot find certificate with SerialNumber='123'.\r\n");

            app.GisMtApi.VerifyNoOutstandingExpectation();
        }
    }
}
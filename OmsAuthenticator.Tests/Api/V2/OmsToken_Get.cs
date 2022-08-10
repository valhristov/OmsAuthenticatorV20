using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OmsAuthenticator.Tests.Api.V2
{
    [TestClass]
    public class OmsToken_Get
    {
        private OmsAuthenticatorApp App { get; }
        private HttpClient Client { get; }

        public OmsToken_Get()
        {
            App = new OmsAuthenticatorApp(@"{
  ""Authenticator"": {
    ""SignDataPath"": "".\\SignData.exe"",
    ""TokenProviders"": {
      ""key1"": {
        ""Adapter"": ""gis-v3"",
        ""Certificate"": ""111"",
        ""Url"": ""https://demo.crpt.ru""
      }
    }
  }
}");
            Client = App.CreateClient();
        }

        private string NewGuid() => Guid.NewGuid().ToString();

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
            var omsConnection = NewGuid();

            App.GisMtApi.SetupGetCertKeyRequest("the data");
            App.GisMtApi.SetupGetTokenRequest(omsConnection, "the data", "the token");

            var result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={NewGuid()}&connectionid={omsConnection}{requestIdParameter}");

            await ResponseShould.BeOkResult(result, "the token");

            //App.GisMtApi.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task Token_Is_Cached()
        {
            // The token key consists of omd id, registration key and request id

            var omsConnection = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();

            App.GisMtApi.SetupGetCertKeyRequest("the data");
            App.GisMtApi.SetupGetTokenRequest(omsConnection, "the data", "the token");

            var result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            await ResponseShould.BeOkResult(result, "the token", requestId);

            result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            await ResponseShould.BeOkResult(result, "the token", requestId);

            result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            await ResponseShould.BeOkResult(result, "the token", requestId);

            //_httpClientMock.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task New_Token_Forced()
        {
            // The token key consists of omd id, registration key and request id

            var omsConnection = NewGuid();
            var omsId = NewGuid();

            App.GisMtApi.SetupGetCertKeyRequest("the data 1");
            App.GisMtApi.SetupGetTokenRequest(omsConnection, "the data 1", "the token 1");
            App.GisMtApi.SetupGetCertKeyRequest("the data 2");
            App.GisMtApi.SetupGetTokenRequest(omsConnection, "the data 2", "the token 2");
            App.GisMtApi.SetupGetCertKeyRequest("the data 3");
            App.GisMtApi.SetupGetTokenRequest(omsConnection, "the data 3", "the token 3");

            var requestId = NewGuid();
            var result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            await ResponseShould.BeOkResult(result, "the token 1", requestId);

            requestId = NewGuid();
            result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            await ResponseShould.BeOkResult(result, "the token 2", requestId);

            requestId = NewGuid();
            result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            await ResponseShould.BeOkResult(result, "the token 3", requestId);

            //_httpClientMock.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task Token_Expires_In_Cache()
        {
            // The token key consists of omd id, registration key and request id

            var omsConnection = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();

            App.GisMtApi.SetupGetCertKeyRequest("the data 1");
            App.GisMtApi.SetupGetTokenRequest(omsConnection, "the data 1", "the token 1");
            App.GisMtApi.SetupGetCertKeyRequest("the data 2");
            App.GisMtApi.SetupGetTokenRequest(omsConnection, "the data 2", "the token 2");
            App.GisMtApi.SetupGetCertKeyRequest("the data 3");
            App.GisMtApi.SetupGetTokenRequest(omsConnection, "the data 3", "the token 3");

            var result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            await ResponseShould.BeOkResult(result, "the token 1", requestId);

            App.Wait(TimeSpan.FromHours(10)); // wait for the token to expire

            result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            await ResponseShould.BeOkResult(result, "the token 2", requestId);

            App.Wait(TimeSpan.FromHours(10)); // wait for the token to expire

            result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            await ResponseShould.BeOkResult(result, "the token 3", requestId);

            //_httpClientMock.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task Current_Token_Is_Returned()
        {
            // Arrange
            var omsConnection = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();

            App.GisMtApi.SetupGetCertKeyRequest("the data");
            App.GisMtApi.SetupGetTokenRequest(omsConnection, "the data", "the token");

            var result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            await ResponseShould.BeOkResult(result, "the token", requestId);

            // Act - no request id parameter
            result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}");

            // Assert
            await ResponseShould.BeOkResult(result, "the token", requestId);

            //_httpClientMock.VerifyNoOutstandingExpectation();
        }

    }
}
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

        private string NewGuid() => Guid.NewGuid().ToString();
        private string NewDataToSign() => Guid.NewGuid().ToString().Replace("-", "");

        private void WaitForTokenToExpire() =>
            App.Wait(Expiration.Add(TimeSpan.FromSeconds(1)));

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
            var dataToSign = NewDataToSign();

            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token");

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
            var dataToSign = NewDataToSign();

            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token");

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

            var dataToSign = NewDataToSign();
            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token 1");
            dataToSign = NewDataToSign();
            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token 2");
            dataToSign = NewDataToSign();
            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token 3");

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

            var dataToSign = NewDataToSign();
            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token 1");
            dataToSign = NewDataToSign();
            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token 2");
            dataToSign = NewDataToSign();
            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token 3");

            var result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            await ResponseShould.BeOkResult(result, "the token 1", requestId);

            WaitForTokenToExpire();

            result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            await ResponseShould.BeOkResult(result, "the token 2", requestId);

            WaitForTokenToExpire();

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
            var dataToSign = NewDataToSign();

            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token");

            var result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            await ResponseShould.BeOkResult(result, "the token", requestId);

            // Act - no request id parameter
            result = await Client.GetAsync($"/api/v2/key1/oms/token?omsid={omsId}&connectionid={omsConnection}");

            // Assert
            await ResponseShould.BeOkResult(result, "the token", requestId);

            //_httpClientMock.VerifyNoOutstandingExpectation();
        }

        // TODO: GIS MT returns errors
    }
}
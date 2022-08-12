using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace OmsAuthenticator.Tests.Api.V1
{
    [TestClass]
    public class OmsToken_Post
    {
        private static readonly TimeSpan Expiration = TimeSpan.FromHours(10);

        public OmsAuthenticatorApp App { get; }
        public HttpClient Client { get; }

        public OmsToken_Post()
        {
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

        private async Task<HttpResponseMessage> PostAsync(object request) =>
            await Client.PostAsync("/oms/token", new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));

        [DebuggerStepThrough]
        private string NewDataToSign() => Guid.NewGuid().ToString().Replace("-", "");

        [DebuggerStepThrough]
        private void WaitForTokenToExpire() => App.Wait(Expiration.Add(TimeSpan.FromSeconds(1)));

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
            using var response = await Client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));

            // Assert
            await ResponseShould.BeBadRequest(response);
        }

        [TestMethod]
        public async Task No_Token_In_Cache()
        {
            var omsConnection = NewGuid();
            var dataToSign = NewDataToSign();

            App.GisMtApi.SetupGetCertKeyRequest(dataToSign);
            App.GisMtApi.SetupGetTokenRequest(omsConnection, dataToSign, "the token 1");

            var result = await PostAsync(new { omsConnection = omsConnection, omsId = NewGuid(), requestId = NewGuid(), });

            await ResponseShould.BeOk(result, "the token 1");

            App.GisMtApi.VerifyNoOutstandingExpectation();
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

            var result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = requestId, });
            await ResponseShould.BeOk(result, "the token");

            result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = requestId, });
            await ResponseShould.BeOk(result, "the token");

            result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = requestId, });
            await ResponseShould.BeOk(result, "the token");

            App.GisMtApi.VerifyNoOutstandingExpectation();
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

            var result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = NewGuid(), });
            await ResponseShould.BeOk(result, "the token 1");

            result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = NewGuid(), });
            await ResponseShould.BeOk(result, "the token 2");

            result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = NewGuid(), });
            await ResponseShould.BeOk(result, "the token 3");

            App.GisMtApi.VerifyNoOutstandingExpectation();
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

            var result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = requestId, });
            await ResponseShould.BeOk(result, "the token 1");

            WaitForTokenToExpire();

            result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = requestId, });
            await ResponseShould.BeOk(result, "the token 2");

            WaitForTokenToExpire();

            result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = requestId, });
            await ResponseShould.BeOk(result, "the token 3");

            App.GisMtApi.VerifyNoOutstandingExpectation();
        }

        [DebuggerStepThrough]
        private string NewGuid() =>
            Guid.NewGuid().ToString();
    }
}
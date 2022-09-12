using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace OmsAuthenticator.Tests.Api.V1
{
    [TestClass]
    public class OmsToken_Get
    {
        private static readonly TimeSpan Expiration = TimeSpan.FromHours(10);

        public OmsAuthenticatorApp App { get; }
        public HttpClient Client { get; }

        [DebuggerStepThrough]
        private string NewGuid() => Guid.NewGuid().ToString();
        [DebuggerStepThrough]
        private string NewDataToSign() => Guid.NewGuid().ToString().Replace("-", "");

        private async Task<HttpResponseMessage> PostAsync(object request) =>
            await Client.PostAsync("/oms/token", new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));

        public OmsToken_Get()
        {
            App = new OmsAuthenticatorApp($@"{{
  ""Authenticator"": {{
    ""SignDataPath"": "".\\SignData.exe"",
    ""TokenProviders"": {{
      ""key1"": {{
        ""Adapter"": ""oms-v3"",
        ""Certificate"": ""integrationtests"",
        ""Url"": ""https://demo.crpt.ru"",
        ""Expiration"": ""{Expiration}""
      }}
    }}
  }}
}}");
            Client = App.CreateClient();
        }

        [DataTestMethod]
        [DataRow("/oms/token")]
        [DataRow("/oms/token?test=best")]
        [DataRow("/oms/token?omsId=xxx")]
        [DataRow("/oms/token?registrationKey=xxx")]
        public async Task GetToken_Invalid_Get(string url)
        {
            // Arrange

            // Act
            using var response = await Client.GetAsync(url);

            // Assert
            await ResponseShould.BeBadRequest(response);
        }

        [TestMethod]
        public async Task No_Token_In_Cache()
        {
            // Act
            var result = await Client.GetAsync($"/oms/token?registrationKey={NewGuid()}&omsId={NewGuid()}");

            // Assert
            await ResponseShould.BeNotFound(result);
        }

        [DataTestMethod]
        [DataRow("other-registration-key", "oms-id")]
        [DataRow("registration-key", "other-oms-id")]
        [DataRow("other-registration-key", "other-oms-id")]
        public async Task Incompatible_Token_In_Cache(string registrationKey, string omsId)
        {
            // Only tokens generated with the same registration key and oms id are compatible

            // Arrange
            var omsConnection = NewGuid();
            App.GisApi.ExpectGetTokenSequence(omsConnection, "the token");

            // Get a token
            var result = await PostAsync(new { registrationKey = registrationKey, omsConnection = omsConnection, omsId = omsId, requestId = NewGuid(), });

            await ResponseShould.BeOk(result, "the token");
            // We have a token in the cache now, we can continue with the test

            // Act
            // We have a different registration key or OMS ID (see parameters)
            result = await Client.GetAsync($"/oms/token?registrationKey=registration-key&omsId=oms-id");

            // Assert
            await ResponseShould.BeNotFound(result);
        }

        [TestMethod]
        public async Task Compatible_Tokens_In_Cache()
        {
            // Only tokens generated with the same registration key and oms id are compatible

            // Arrange
            var omsId = NewGuid();
            var omsConnection = NewGuid();
            var registrationKey = NewGuid();

            App.GisApi.ExpectGetTokenSequence(omsConnection, "the token");

            // Get a token
            var result = await PostAsync(new { omsConnection = omsConnection, omsId = omsId, requestId = NewGuid(), registrationKey = registrationKey, });

            await ResponseShould.BeOk(result, "the token");
            // We have a token in the cache now, we can continue with the test

            // Act
            result = await Client.GetAsync($"/oms/token?registrationKey={registrationKey}&omsId={omsId}");

            // Assert
            await ResponseShould.BeOk(result, "the token");
        }
    }
}
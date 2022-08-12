using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OmsAuthenticator.Tests.Api.V1
{
    [TestClass]
    public class OmsToken_Get
    {
        private static readonly TimeSpan Expiration = TimeSpan.FromHours(10);

        public OmsAuthenticatorApp App { get; }
        public HttpClient Client { get; }

        public OmsToken_Get()
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
    }
}
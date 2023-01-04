using System.Diagnostics;
using OmsAuthenticator.Tests.Clients;
using OmsAuthenticator.Tests.Helpers;
using Xunit;

namespace OmsAuthenticator.Tests
{
    public class Api_V2
    {
        private const string OmsProviderKey = "oms-provider";
        private const string TrueProviderKey = "true-provider";
        private static readonly TimeSpan Expiration = TimeSpan.FromHours(10);

        private readonly HttpClient _httpClient;
        private readonly OmsAuthenticatorApp _app;

        [DebuggerStepThrough]
        private string NewGuid() => Guid.NewGuid().ToString();

        public Api_V2()
        {
            // Using "integrationtests" for Certificate to short-cirquit the signer
            _app = new OmsAuthenticatorApp($@"{{
  ""Authenticator"": {{
    ""SignDataPath"": "".\\SignData\\SignData.exe"",
    ""TokenProviders"": {{
      ""{OmsProviderKey}"": {{
        ""Adapter"": ""oms-v3"",
        ""Certificate"": ""integrationtests"",
        ""Url"": ""https://demo.crpt.ru"",
        ""Expiration"": ""{Expiration}""
      }},
      ""{TrueProviderKey}"": {{
        ""Adapter"": ""true-v3"",
        ""Certificate"": ""integrationtests"",
        ""Url"": ""https://demo.crpt.ru"",
        ""Expiration"": ""{Expiration}""
      }}
    }}
  }}
}}");
            _httpClient = _app.CreateClient();
        }

        [Theory]
        [InlineData("/api/v2/" + OmsProviderKey + "/oms/token", "omsId")]
        [InlineData("/api/v2/" + OmsProviderKey + "/oms/token?omsid=best", "connectionId")]
        [InlineData("/api/v2/" + OmsProviderKey + "/oms/token?connectionid=best", "omsId")]
        [InlineData("/api/v2/" + OmsProviderKey + "/oms/token?omsId=best&requestId=1", "connectionId")]
        [InlineData("/api/v2/" + OmsProviderKey + "/oms/token?connectionid=best&requestid=1", "omsId")]
        public async Task Invalid_Request_Oms(string url, string expectedMissingParameterName)
        {
            // Arrange

            // Act
            var response = await OmsAuthenticatorClientV2.GetOmsAuthenticatorResponse(
                await _httpClient.GetAsync(url, CancellationToken.None));

            // Assert
            response.ShouldBeBadRequest($"Query string parameter '{expectedMissingParameterName}' is required.");
        }

        [Fact(Skip = "True token API has only one parameter that is optional, therefore there is no validation.")]
        public void Invalid_Request_True()
        {
        }

        [Theory]
        [InlineData("&requestid=1")] // we get token with request id
        [InlineData("")] // we get token without request id
        public async Task Unsupported_Token_Type(string requestIdParameter)
        {
            // Note the path contains "true" instead of "oms". This means the URL is for
            // true api tokens. Valid OMS request parameters provided to the TRUE API
            // should generate response with status code 422.

            // Arrange

            // Act
            var response = await OmsAuthenticatorClientV2.GetOmsAuthenticatorResponse(
                await _httpClient.GetAsync($"/api/v2/{OmsProviderKey}/true/token?omsid={NewGuid()}&connectionid={NewGuid()}{requestIdParameter}"));

            // Assert
            response.ShouldBeUnprocessableEntity("Adapter 'oms-v3' does not support tokens of type 'TrueApi'. Are you using the wrong URL?");
        }
    }
}

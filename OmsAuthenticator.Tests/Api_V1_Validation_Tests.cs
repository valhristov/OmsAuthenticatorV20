using System.Diagnostics;
using OmsAuthenticator.Tests.Clients;
using OmsAuthenticator.Tests.Helpers;
using Xunit;

namespace OmsAuthenticator.Tests
{
    public class Api_V1
    {
        private const string GisProviderKey = "gis-provider";
        private const string DtabacProviderKey = "dtabac-provider";
        private static readonly TimeSpan Expiration = TimeSpan.FromHours(10);

        private readonly HttpClient _httpClient;
        private readonly OmsAuthenticatorApp _app;

        [DebuggerStepThrough]
        private string NewGuid() => Guid.NewGuid().ToString();

        public Api_V1()
        {
            // Using "integrationtests" for Certificate to short-cirquit the signer
            _app = new OmsAuthenticatorApp($@"{{
  ""Authenticator"": {{
    ""SignDataPath"": "".\\SignData\\SignData.exe"",
    ""TokenProviders"": {{
      ""{GisProviderKey}"": {{
        ""Adapter"": ""gis-v3"",
        ""Certificate"": ""integrationtests"",
        ""Url"": ""https://demo.crpt.ru"",
        ""Expiration"": ""{Expiration}""
      }},
      ""{DtabacProviderKey}"": {{
        ""Adapter"": ""dtabac-v0"",
        ""Url"": ""https://demo.crpt.ru"",
        ""Expiration"": ""{Expiration}""
      }}
    }}
  }}
}}");
            _httpClient = _app.CreateClient();
        }

        [Theory]
        [InlineData("/api/v1/" + GisProviderKey + "/oms/token", "omsId")]
        [InlineData("/api/v1/" + GisProviderKey + "/oms/token?omsid=best", "registrationKey")]
        [InlineData("/api/v1/" + GisProviderKey + "/oms/token?connectionid=best", "omsId")]
        [InlineData("/api/v1/" + GisProviderKey + "/oms/token?omsId=best&requestId=1", "registrationKey")]
        [InlineData("/api/v1/" + GisProviderKey + "/oms/token?connectionid=best&requestid=1", "omsId")]
        [InlineData("/oms/token", "omsId")]
        [InlineData("/oms/token?test=best", "omsId")]
        [InlineData("/oms/token?omsId=xxx", "registrationKey")]
        [InlineData("/oms/token?registrationKey=xxx", "omsId")]
        public async Task Invalid_Request(string url, string expectedMissingParameterName)
        {
            // Arrange

            // Act
            var response = await OmsAuthenticatorClientV1.GetOmsAuthenticatorResponse(
                await _httpClient.GetAsync(url, CancellationToken.None));

            // Assert
            response.ShouldBeBadRequest($"{expectedMissingParameterName} query string parameter is required.");
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
            var response = await OmsAuthenticatorClientV1.GetOmsAuthenticatorResponse(
                await _httpClient.GetAsync($"/api/v2/{DtabacProviderKey}/true/token?omsid={NewGuid()}&connectionid={NewGuid()}{requestIdParameter}"));

            // Assert
            response.ShouldBeNotFound();
        }
    }
}

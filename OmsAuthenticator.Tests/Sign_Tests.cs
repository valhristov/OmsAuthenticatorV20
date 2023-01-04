using OmsAuthenticator.Tests.Clients;
using OmsAuthenticator.Tests.Helpers;
using Xunit;

namespace OmsAuthenticator.Tests
{
    public class Sign_Tests
    {
        // The key of the token provider configuration to use
        private const string ProviderKey = "provider";
        private static readonly TimeSpan Expiration = TimeSpan.FromHours(10);

        private readonly HttpClient _httpClient;
        private readonly OmsAuthenticatorApp _app;

        public static object[][] AuthenticatorClients = new[]
        {
            new [] { new Func<HttpClient, ISignatureClient>(httpClient => new OmsAuthenticatorClientV2(httpClient, ProviderKey)) },
            new [] { new Func<HttpClient, ISignatureClient>(httpClient => new OmsAuthenticatorClientLegacy(httpClient)) },
        };

        public Sign_Tests()
        {
            // Using "integrationtests" for Certificate to short-cirquit the signer
            _app = new OmsAuthenticatorApp($@"{{
  ""Authenticator"": {{
    ""SignDataPath"": "".\\SignData\\SignData.exe"",
    ""TokenProviders"": {{
      ""{ProviderKey}"": {{
        ""Adapter"": ""gis-v3"",
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
        [MemberData(nameof(AuthenticatorClients))]
        public async Task SignData_Returns_Error(Func<HttpClient, ISignatureClient> getClient)
        {
            // Arrange
            // Certificate is not existing one, we will get errors
            var app = new OmsAuthenticatorApp($@"{{
  ""Authenticator"": {{
    ""SignDataPath"": "".\\SignData\\SignData.exe"",
    ""TokenProviders"": {{
      ""{ProviderKey}"": {{
        ""Adapter"": ""gis-v3"",
        ""Certificate"": ""does-not-exist"",
        ""Url"": ""https://demo.crpt.ru"",
        ""Expiration"": ""{Expiration}""
      }}
    }}
  }}
}}");
            var client = getClient(app.CreateClient());

            // Act
            var response = await client.Sign("some value");
            // Assert
            response.ShouldBeUnprocessableEntity(
                $"[SignData.exe] Cannot find certificate with SerialNumber='does-not-exist'.\r\n");
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task Sign_Success(Func<HttpClient, ISignatureClient> getClient)
        {
            // Arrange
            var client = getClient(_httpClient);

            // Act
            var response = await client.Sign("somevalue");
            // Assert
            response.ShouldBeOk("signed:somevalue");
        }
    }
}

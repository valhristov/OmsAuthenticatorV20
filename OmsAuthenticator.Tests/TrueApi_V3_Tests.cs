using System.Diagnostics;
using OmsAuthenticator.Tests.Clients;
using OmsAuthenticator.Tests.Helpers;
using Xunit;

namespace OmsAuthenticator.Tests
{
    public class TrueApi_V3_Tests
    {
        // The key of the token provider configuration to use
        private const string ProviderKey = "provider";
        private static readonly TimeSpan Expiration = TimeSpan.FromHours(10);
        private readonly OmsAuthenticatorApp _app;
        private readonly HttpClient _httpClient;
        private readonly TrueApi _tokenApi;

        [DebuggerStepThrough]
        private string NewGuid() => Guid.NewGuid().ToString();

        public static object[][] AuthenticatorClients = new[]
        {
            new [] { new Func<HttpClient, ITrueTokenClient>(httpClient => new OmsAuthenticatorClientV2(httpClient, ProviderKey)) },
        };

        public TrueApi_V3_Tests()
        {
            // Using "integrationtests" for Certificate to short-cirquit the signer
            _app = new OmsAuthenticatorApp($@"{{
  ""Authenticator"": {{
    ""SignDataPath"": "".\\SignData\\SignData.exe"",
    ""TokenProviders"": {{
      ""{ProviderKey}"": {{
        ""Adapter"": ""true-v3"",
        ""Certificate"": ""integrationtests"",
        ""Url"": ""https://demo.crpt.ru"",
        ""Expiration"": ""{Expiration}""
      }}
    }}
  }}
}}");
            _httpClient = _app.CreateClient();
            _tokenApi = _app.TrueApi;
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task Get_Last_Token(Func<HttpClient, ITrueTokenClient> getClient)
        {
            // Arrange
            _tokenApi.SetupTokenSuccess();

            var client = getClient(_httpClient);

            var requestId = NewGuid();
            await client.GetTrueToken(requestId);
            _tokenApi.GetTokenExecuted(times: 1); // Sanity check

            // Act
            var response = await client.GetLastTrueToken();

            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(0), requestId);
            _tokenApi.GetTokenExecuted(times: 1);
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task Force_New_Token(Func<HttpClient, ITrueTokenClient> getClient)
        {
            // Here we ensure that only the first token request will obtain
            // a new token. Each next request should use the existing token
            // from the cache.

            // Arrange
            _tokenApi.SetupTokenSuccess();

            var client = getClient(_httpClient);

            // Arrange & Act
            var requestId = NewGuid();
            var response = await client.GetTrueToken(requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(0), requestId);
            _tokenApi.GetTokenExecuted(times: 1);

            // Arrange & Act
            requestId = NewGuid();
            response = await client.GetTrueToken(requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(1), requestId);
            _tokenApi.GetTokenExecuted(times: 2);

            // Arrange & Act
            requestId = NewGuid();
            response = await client.GetTrueToken(requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(2), requestId);
            _tokenApi.GetTokenExecuted(times: 3);
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task Token_Is_Cached(Func<HttpClient, ITrueTokenClient> getClient)
        {
            // Here we ensure that only the first token request will obtain
            // a new token. Each next request should use the existing token
            // from the cache.

            // Arrange
            var requestId = NewGuid();

            _tokenApi.SetupTokenSuccess();

            var client = getClient(_httpClient);

            // Act
            var response = await client.GetTrueToken(requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(0), requestId);
            _tokenApi.GetTokenExecuted(times: 1);

            // Act
            response = await client.GetTrueToken(requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(0), requestId);
            _tokenApi.GetTokenExecuted(times: 1);

            // Act
            response = await client.GetTrueToken(requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(0), requestId);
            _tokenApi.GetTokenExecuted(times: 1);
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task Token_Is_Expired(Func<HttpClient, ITrueTokenClient> getClient)
        {
            // Arrange
            var requestId = NewGuid();

            _tokenApi.SetupTokenSuccess();

            var client = getClient(_httpClient);

            // Act
            var response = await client.GetTrueToken(requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(0), requestId);
            _tokenApi.GetTokenExecuted(times: 1);

            // Act
            _app.Wait(Expiration.Add(TimeSpan.FromMinutes(1)));
            response = await client.GetTrueToken(requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(1), requestId);
            _tokenApi.GetTokenExecuted(times: 2);

            // Act
            _app.Wait(Expiration.Add(TimeSpan.FromMinutes(1)));
            response = await client.GetTrueToken(requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(2), requestId);
            _tokenApi.GetTokenExecuted(times: 3);
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task FirstStep_Returns_Error(Func<HttpClient, ITrueTokenClient> getClient)
        {
            // Arrange
            var requestId = NewGuid();

            _tokenApi.SetupCertKeyFailure();

            var client = getClient(_httpClient);

            // Act
            var response = await client.GetTrueToken(requestId);
            // Assert
            response.ShouldBeUnprocessableEntity(
                "[GET https://demo.crpt.ru/api/v3/auth/cert/key] Response was 400 with content 'boom'");
            _tokenApi.FirstStepExecuted(times: 1);

            // Act
            response = await client.GetTrueToken(requestId);
            // Assert
            response.ShouldBeUnprocessableEntity(
                "[GET https://demo.crpt.ru/api/v3/auth/cert/key] Response was 400 with content 'boom'");
            _tokenApi.FirstStepExecuted(times: 2);
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task SecondStep_Returns_Error(Func<HttpClient, ITrueTokenClient> getClient)
        {
            // Arrange
            var requestId = NewGuid();

            _tokenApi.SetupTokenFailure();

            var client = getClient(_httpClient);

            // Act
            var response = await client.GetTrueToken(requestId);
            // Assert
            response.ShouldBeUnprocessableEntity(
                $"[POST https://demo.crpt.ru/api/v3/true-api/auth/simpleSignIn] Response was 400 with content 'boom'");
            _tokenApi.GetTokenExecuted(times: 1);

            // Act
            response = await client.GetTrueToken(requestId);
            // Assert
            response.ShouldBeUnprocessableEntity(
                $"[POST https://demo.crpt.ru/api/v3/true-api/auth/simpleSignIn] Response was 400 with content 'boom'");
            _tokenApi.GetTokenExecuted(times: 2);
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task SignData_Returns_Error(Func<HttpClient, ITrueTokenClient> getClient)
        {
            // Arrange
            // Certificate is not existing one, we will get errors
            var app = new OmsAuthenticatorApp($@"{{
  ""Authenticator"": {{
    ""SignDataPath"": "".\\SignData\\SignData.exe"",
    ""TokenProviders"": {{
      ""{ProviderKey}"": {{
        ""Adapter"": ""true-v3"",
        ""Certificate"": ""does-not-exist"",
        ""Url"": ""https://demo.crpt.ru"",
        ""Expiration"": ""{Expiration}""
      }}
    }}
  }}
}}");
            var tokenApi = app.TrueApi;
            var httpClient = app.CreateClient();

            var requestId = NewGuid();

            tokenApi.SetupTokenSuccess();

            var client = getClient(httpClient);

            // Act
            var response = await client.GetTrueToken(requestId);
            // Assert
            response.ShouldBeUnprocessableEntity(
                $"[SignData.exe] Cannot find certificate with SerialNumber='does-not-exist'.\r\n");
            tokenApi.FirstStepExecuted(times: 1);

            // Act
            response = await client.GetTrueToken(requestId);
            // Assert
            response.ShouldBeUnprocessableEntity(
                $"[SignData.exe] Cannot find certificate with SerialNumber='does-not-exist'.\r\n");
            tokenApi.FirstStepExecuted(times: 2);
        }
    }
}

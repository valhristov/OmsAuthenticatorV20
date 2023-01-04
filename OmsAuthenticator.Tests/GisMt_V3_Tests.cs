using System.Diagnostics;
using OmsAuthenticator.Tests.Clients;
using OmsAuthenticator.Tests.Helpers;
using Xunit;

namespace OmsAuthenticator.Tests
{
    public class GisMt_V3_Tests
    {
        // The key of the token provider configuration to use
        private const string ProviderKey = "provider";
        private static readonly TimeSpan Expiration = TimeSpan.FromHours(10);

        private readonly HttpClient _httpClient;
        private readonly OmsAuthenticatorApp _app;
        private readonly GisApi _tokenApi;

        [DebuggerStepThrough]
        private string NewGuid() => Guid.NewGuid().ToString();

        public static object[][] AuthenticatorClients = new[]
        {
            new [] { new Func<HttpClient, IOmsTokenClient>(httpClient => new OmsAuthenticatorClientV2(httpClient, ProviderKey)) },
            new [] { new Func<HttpClient, IOmsTokenClient>(httpClient => new OmsAuthenticatorClientV1(httpClient, ProviderKey)) },
            new [] { new Func<HttpClient, IOmsTokenClient>(httpClient => new OmsAuthenticatorClientLegacy(httpClient)) },
        };

        public GisMt_V3_Tests()
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
            _tokenApi = _app.GisApi;
            _httpClient = _app.CreateClient();
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task Get_Last_Token(Func<HttpClient, IOmsTokenClient> getClient)
        {
            // Arrange
            var omsConnection = NewGuid();
            var omsId = NewGuid();

            _tokenApi.SetupSuccess(omsConnection);

            var client = getClient(_httpClient);

            var requestId = NewGuid();
            await client.GetOmsToken(omsId, omsConnection, requestId); // just load a token in the cache
            _tokenApi.GetTokenExecuted(times: 1); // sanity check

            // Act
            var response = await client.GetLastOmsToken(omsId, omsConnection);

            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(0), requestId);
            _tokenApi.GetTokenExecuted(times: 1); // no new requests
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task Force_New_Token(Func<HttpClient, IOmsTokenClient> getClient)
        {
            // Arrange
            var omsConnection = NewGuid();
            var omsId = NewGuid();

            _tokenApi.SetupSuccess(omsConnection);

            var client = getClient(_httpClient);

            // Arrange & Act
            var requestId = NewGuid();
            var response = await client.GetOmsToken(omsId, omsConnection, requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(0), requestId);
            _tokenApi.GetTokenExecuted(times: 1);

            // Arrange & Act
            requestId = NewGuid();
            response = await client.GetOmsToken(omsId, omsConnection, requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(1), requestId);
            _tokenApi.GetTokenExecuted(times: 2);

            // Arrange & Act
            requestId = NewGuid();
            response = await client.GetOmsToken(omsId, omsConnection, requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(2), requestId);
            _tokenApi.GetTokenExecuted(times: 3);
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task Token_Is_Cached(Func<HttpClient, IOmsTokenClient> getClient)
        {
            // Arrange
            var omsConnection = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();

            _tokenApi.SetupSuccess(omsConnection);

            var client = getClient(_httpClient);

            // Act
            var response = await client.GetOmsToken(omsId, omsConnection, requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(0), requestId);
            _tokenApi.GetTokenExecuted(times: 1);

            // Act
            response = await client.GetOmsToken(omsId, omsConnection, requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(0), requestId);
            _tokenApi.GetTokenExecuted(times: 1);

            // Act
            response = await client.GetOmsToken(omsId, omsConnection, requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(0), requestId);
            _tokenApi.GetTokenExecuted(times: 1);
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task Token_For_Different_Connection(Func<HttpClient, IOmsTokenClient> getClient)
        {
            // Arrange
            var omsConnection1 = NewGuid();
            var omsConnection2 = NewGuid();
            var omsConnection3 = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();

            _tokenApi.SetupSuccess(omsConnection1);
            _tokenApi.SetupSuccess(omsConnection2);
            _tokenApi.SetupSuccess(omsConnection3);

            var client = getClient(_httpClient);

            // Act
            var response = await client.GetOmsToken(omsId, omsConnection1, requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(0), requestId);
            _tokenApi.GetTokenExecuted(times: 1);

            // Act
            response = await client.GetOmsToken(omsId, omsConnection2, requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(1), requestId);
            _tokenApi.GetTokenExecuted(times: 2);

            // Act
            response = await client.GetOmsToken(omsId, omsConnection3, requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(2), requestId);
            _tokenApi.GetTokenExecuted(times: 3);
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task Token_For_Different_OmsId(Func<HttpClient, IOmsTokenClient> getClient)
        {
            // Arrange
            var omsConnection = NewGuid();
            var omsId1 = NewGuid();
            var omsId2 = NewGuid();
            var omsId3 = NewGuid();
            var requestId = NewGuid();

            _tokenApi.SetupSuccess(omsConnection);

            var client = getClient(_httpClient);

            // Act
            var response = await client.GetOmsToken(omsId1, omsConnection, requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(0), requestId);
            _tokenApi.GetTokenExecuted(times: 1);

            // Act
            response = await client.GetOmsToken(omsId2, omsConnection, requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(1), requestId);
            _tokenApi.GetTokenExecuted(times: 2);

            // Act
            response = await client.GetOmsToken(omsId3, omsConnection, requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(2), requestId);
            _tokenApi.GetTokenExecuted(times: 3);
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task Token_Is_Expired(Func<HttpClient, IOmsTokenClient> getClient)
        {
            // Arrange
            var omsConnection = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();

            _tokenApi.SetupSuccess(omsConnection);

            var client = getClient(_httpClient);

            // Act
            var response = await client.GetOmsToken(omsId, omsConnection, requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(0), requestId);
            _tokenApi.GetTokenExecuted(times: 1);

            // Act
            _app.Wait(Expiration.Add(TimeSpan.FromMinutes(1)));
            response = await client.GetOmsToken(omsId, omsConnection, requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(1), requestId);
            _tokenApi.GetTokenExecuted(times: 2);

            // Act
            _app.Wait(Expiration.Add(TimeSpan.FromMinutes(1)));
            response = await client.GetOmsToken(omsId, omsConnection, requestId);
            // Assert
            response.ShouldBeOk(_tokenApi.IssuedTokens.ElementAt(2), requestId);
            _tokenApi.GetTokenExecuted(times: 3);
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task FirstStep_Returns_Error(Func<HttpClient, IOmsTokenClient> getClient)
        {
            // Arrange
            var omsConnection = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();

            _tokenApi.SetupFirstStepFailure();

            var client = getClient(_httpClient);

            // Act
            var response = await client.GetOmsToken(omsId, omsConnection, requestId);
            // Assert
            response.ShouldBeUnprocessableEntity(
                "[GET https://demo.crpt.ru/api/v3/auth/cert/key] Response was 400 with content 'boom'");
            _tokenApi.FirstStepExecuted(times: 1);

            // Act
            response = await client.GetOmsToken(omsId, omsConnection, requestId);
            // Assert
            response.ShouldBeUnprocessableEntity(
                "[GET https://demo.crpt.ru/api/v3/auth/cert/key] Response was 400 with content 'boom'");
            _tokenApi.FirstStepExecuted(times: 2);
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task SecondStep_Returns_Error(Func<HttpClient, IOmsTokenClient> getClient)
        {
            // Arrange
            var omsConnection = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();

            _tokenApi.SetupSecondStepFailure(omsConnection);

            var client = getClient(_httpClient);

            // Act
            var response = await client.GetOmsToken(omsId, omsConnection, requestId);
            // Assert
            response.ShouldBeUnprocessableEntity(
                $"[POST https://demo.crpt.ru/api/v3/auth/cert/{omsConnection}] Response was 400 with content 'boom'");
            _tokenApi.GetTokenExecuted(times: 1);

            // Act
            response = await client.GetOmsToken(omsId, omsConnection, requestId);
            // Assert
            response.ShouldBeUnprocessableEntity(
                $"[POST https://demo.crpt.ru/api/v3/auth/cert/{omsConnection}] Response was 400 with content 'boom'");
            _tokenApi.GetTokenExecuted(times: 2);
        }

        [Theory]
        [MemberData(nameof(AuthenticatorClients))]
        public async Task SignData_Returns_Error(Func<HttpClient, IOmsTokenClient> getClient)
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
            var tokenApi = app.GisApi;
            var httpClient = app.CreateClient();

            var omsConnection = NewGuid();
            var omsId = NewGuid();
            var requestId = NewGuid();

            tokenApi.SetupSuccess(omsConnection);

            var client = getClient(httpClient);

            // Act
            var response = await client.GetOmsToken(omsId, omsConnection, requestId);
            // Assert
            response.ShouldBeUnprocessableEntity(
                $"[SignData.exe] Cannot find certificate with SerialNumber='does-not-exist'.\r\n");
            tokenApi.FirstStepExecuted(times: 1);

            // Act
            response = await client.GetOmsToken(omsId, omsConnection, requestId);
            // Assert
            response.ShouldBeUnprocessableEntity(
                $"[SignData.exe] Cannot find certificate with SerialNumber='does-not-exist'.\r\n");
            tokenApi.FirstStepExecuted(times: 2);
        }
    }
}

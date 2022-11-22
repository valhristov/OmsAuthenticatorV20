using System.Diagnostics;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OmsAuthenticator.Tests.Api.V2
{
    [TestClass]
    public class GetTrueApiTokenSpec
    {
        // The key of the token provider configuration to use
        private const string ProviderKey = "key1";
        private static readonly TimeSpan Expiration = TimeSpan.FromHours(10);

        private OmsAuthenticatorApp App { get; }
        private HttpClient Client { get; }

        public GetTrueApiTokenSpec()
        {
            // Using "integrationtests" for Certificate to short-cirquit the signer
            App = new OmsAuthenticatorApp($@"{{
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
            Client = App.CreateClient();
        }

        [DebuggerStepThrough]
        private string NewGuid() => Guid.NewGuid().ToString();

        [DebuggerStepThrough]
        private void WaitForTokenToExpire() => App.Wait(Expiration.Add(TimeSpan.FromSeconds(1)));

        [DataTestMethod]
        [DataRow("&requestid=1")] // we get token with request id
        [DataRow("")] // we get token without request id
        public async Task Unsupported_Token_Provider(string requestIdParameter)
        {
            // Note the path contains "oms" instead of "true". This means the URL is for
            // OMS tokens. Valid TRUE API request parameters provided to the OMS API
            // should generate response with status code 422.

            // Arrange

            // Act
            var result = await Client.GetAsync($"/api/v2/{ProviderKey}/oms/token?omsid={NewGuid()}&connectionid={NewGuid()}{requestIdParameter}");

            // Assert
            await ResponseShould.BeUnprocessableEntity(result, "Adapter 'true-v3' does not support tokens of type 'Oms'. Are you using the wrong URL?");
        }

        [DataTestMethod]
        [DataRow("&requestid=1")] // we get token with request id
        [DataRow("")] // we get token without request id
        public async Task No_Token_In_Cache(string requestIdParameter)
        {
            // In this scenario the cache is empty. Requesting token with
            // and without requestid parameter should return a new token.

            // Arrange
            App.TrueApi.ExpectGetTokenSequence("the token");

            // Act
            var result = await Client.GetAsync($"/api/v2/{ProviderKey}/true/token?{requestIdParameter}");

            // Assert
            await ResponseShould.BeOk(result, "the token");

            App.TrueApi.VerifyNoOutstandingExpectation();
        }

        [DataTestMethod]
        [DataRow("&requestid=1")] // we get token with request id
        [DataRow("")] // we get token without request id
        public async Task Expired_Token_In_Cache(string requestIdParameter)
        {
            // In this scenario the cache contains an expired token. Requesting token with
            // and without requestid parameter should return a new token.

            // Arrange
            App.TrueApi.ExpectGetTokenSequence("the token 1");
            App.TrueApi.ExpectGetTokenSequence("the token 2");

            // Cache a new token
            await Client.GetAsync($"/api/v2/{ProviderKey}/true/token?{requestIdParameter}");

            WaitForTokenToExpire();

            // Act
            var result = await Client.GetAsync($"/api/v2/{ProviderKey}/true/token?{requestIdParameter}");

            // Assert
            await ResponseShould.BeOk(result, "the token 2");

            App.TrueApi.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task Token_Is_Cached()
        {
            // Here we ensure that only the first token request will obtain
            // a new token. Each next request should use the existing token
            // from the cache.

            // Arrange
            var omsId = NewGuid();
            var requestId = NewGuid();

            App.TrueApi.ExpectGetTokenSequence("the token");

            // Act
            var result = await Client.GetAsync($"/api/v2/{ProviderKey}/true/token?omsid={omsId}&requestid={requestId}");
            // Assert
            await ResponseShould.BeOk(result, "the token", requestId);

            // Act
            result = await Client.GetAsync($"/api/v2/{ProviderKey}/true/token?omsid={omsId}&requestid={requestId}");
            // Assert
            await ResponseShould.BeOk(result, "the token", requestId);

            // Act
            result = await Client.GetAsync($"/api/v2/{ProviderKey}/true/token?omsid={omsId}&requestid={requestId}");
            // Assert
            await ResponseShould.BeOk(result, "the token", requestId);

            App.TrueApi.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task New_Token_Forced()
        {
            // Specifying a requestid that is different than the one of the
            // existing token in the cache will force obtaining a new token.

            // Arrange
            var omsId = NewGuid();

            App.TrueApi.ExpectGetTokenSequence("the token 1");
            App.TrueApi.ExpectGetTokenSequence("the token 2");
            App.TrueApi.ExpectGetTokenSequence("the token 3");

            // Act
            var requestId = NewGuid();
            var result = await Client.GetAsync($"/api/v2/{ProviderKey}/true/token?omsid={omsId}&requestid={requestId}");
            // Assert
            await ResponseShould.BeOk(result, "the token 1", requestId);

            // Act
            requestId = NewGuid();
            result = await Client.GetAsync($"/api/v2/{ProviderKey}/true/token?omsid={omsId}&requestid={requestId}");
            // Assert
            await ResponseShould.BeOk(result, "the token 2", requestId);

            // Act
            requestId = NewGuid();
            result = await Client.GetAsync($"/api/v2/{ProviderKey}/true/token?omsid={omsId}&requestid={requestId}");
            // Assert
            await ResponseShould.BeOk(result, "the token 3", requestId);

            App.TrueApi.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task Existing_Token_Is_Returned()
        {
            // When requestid is not provided, we return the token that we have
            // in the cache.

            // Arrange
            var omsId = NewGuid();
            var requestId = NewGuid();

            App.TrueApi.ExpectGetTokenSequence("the token");

            // Put a token in cache
            var result = await Client.GetAsync($"/api/v2/{ProviderKey}/true/token?omsid={omsId}&requestid={requestId}");
            await ResponseShould.BeOk(result, "the token", requestId);

            // Act - no request id parameter, we should return existing token
            result = await Client.GetAsync($"/api/v2/{ProviderKey}/true/token?omsid={omsId}");

            // Assert
            await ResponseShould.BeOk(result, "the token", requestId);

            App.TrueApi.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task GetCertKey_Returns_Error()
        {
            // The errors from TRUE API are relayed to our responses with nice error messages.

            // Arrange
            var omsId = NewGuid();

            App.TrueApi.ExpectGetCertKeyRequest(HttpStatusCode.BadRequest, "some error 1");
            App.TrueApi.ExpectGetCertKeyRequest(HttpStatusCode.BadRequest, "some error 2");

            // Act
            var result = await Client.GetAsync($"/api/v2/{ProviderKey}/true/token?omsid={omsId}");
            // Assert
            await ResponseShould.BeUnprocessableEntity(result, "[GET https://demo.crpt.ru/api/v3/auth/cert/key] Response was 400 with content 'some error 1'");

            // Act
            result = await Client.GetAsync($"/api/v2/{ProviderKey}/true/token?omsid={omsId}");
            // Assert
            await ResponseShould.BeUnprocessableEntity(result, "[GET https://demo.crpt.ru/api/v3/auth/cert/key] Response was 400 with content 'some error 2'");

            // Both TRUE API requests are invoked. This means that our second request invoked the API
            App.TrueApi.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task GetToken_Returns_Error()
        {
            // The errors from TRUE API are relayed to our responses with nice error messages.

            // Arrange
            App.TrueApi.ExpectGetTokenSequence(HttpStatusCode.BadRequest, "some error 1");
            App.TrueApi.ExpectGetTokenSequence(HttpStatusCode.BadRequest, "some error 2");

            // Act
            var result = await Client.GetAsync($"/api/v2/{ProviderKey}/true/token?");
            // Assert
            await ResponseShould.BeUnprocessableEntity(result, $"[POST https://demo.crpt.ru/api/v3/true-api/auth/simpleSignIn] Response was 400 with content 'some error 1'");

            // Act
            result = await Client.GetAsync($"/api/v2/{ProviderKey}/true/token?");
            // Assert
            await ResponseShould.BeUnprocessableEntity(result, $"[POST https://demo.crpt.ru/api/v3/true-api/auth/simpleSignIn] Response was 400 with content 'some error 2'");

            // All TRUE API requests are invoked. This means that our second request invoked the API
            App.TrueApi.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task SignData_Returns_Error()
        {
            // The errors from SignData are relayed to our responses with nice error messages.

            // Arrange
            // Using custom app because we have different certificate configuration for this test
            var app = new OmsAuthenticatorApp($@"{{
  ""Authenticator"": {{
    ""SignDataPath"": "".\\SignData\\SignData.exe"",
    ""TokenProviders"": {{
      ""key1"": {{
        ""Adapter"": ""true-v3"",
        ""Certificate"": ""123"",
        ""Url"": ""https://demo.crpt.ru"",
        ""Expiration"": ""{Expiration}""
      }}
    }}
  }}
}}");

            app.TrueApi.ExpectGetCertKeyRequest();

            // Act
            var result = await app.CreateClient().GetAsync($"/api/v2/{ProviderKey}/true/token?");

            // Assert
            await ResponseShould.BeUnprocessableEntity(result, "[SignData.exe] Cannot find certificate with SerialNumber='123'.\r\n");

            app.TrueApi.VerifyNoOutstandingExpectation();
        }
    }
}
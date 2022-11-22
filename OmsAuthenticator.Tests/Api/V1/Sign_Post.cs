using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OmsAuthenticator.Api.V1;

namespace OmsAuthenticator.Tests.Api.V1
{
    [TestClass]
    public class Sign_Post
    {
        // The key of the token provider configuration to use
        private const string ProviderKey = "key1";

        public OmsAuthenticatorApp App { get; }
        public HttpClient Client { get; }

        public Sign_Post()
        {
            // Using "integrationtests" for Certificate to short-cirquit the signer
            App = new OmsAuthenticatorApp($@"{{
  ""Authenticator"": {{
    ""SignDataPath"": "".\\SignData\\SignData.exe"",
    ""TokenProviders"": {{
      ""{ProviderKey}"": {{
        ""Adapter"": ""oms-v3"",
        ""Certificate"": ""integrationtests"",
        ""Url"": ""https://demo.crpt.ru"",
        ""Expiration"": ""00:00:00""
      }}
    }}
  }}
}}");
            Client = App.CreateClient();
        }

        private async Task<HttpResponseMessage> PostAsync(object request) =>
            await Client.PostAsync($"/api/v1/signature", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        [DataTestMethod]
        [DataRow("")]
        [DataRow("{}")]
        [DataRow("{ \"test\": \"best\" }")]
        [DataRow("{ \"payloadBase64\": null }")]
        public async Task Invalid_Request(string body)
        {
            // Arrange

            // Act
            var response = await Client.PostAsync($"/api/v1/signature", new StringContent(body, Encoding.UTF8, "application/json"));

            // Assert
            await ResponseShould.BeBadRequest(response);
        }

        [TestMethod]
        public async Task Data_Signed()
        {
            // Arrange

            // Act
            var response = await PostAsync(new { payloadBase64 = "testbest", });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var signatureResponse = JsonSerializer.Deserialize<SignatureResponseV1>(content)!;

            signatureResponse.Should().NotBeNull();
            // we use "integrationtests" for certificate and the signer returns the input with "signed:" prefix
            signatureResponse.Signature.Should().Be("signed:testbest");
            signatureResponse.Errors.Should().BeNull();
        }
    }
}

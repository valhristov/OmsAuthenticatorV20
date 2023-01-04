using System.Text;
using System.Text.Json;
using OmsAuthenticator.Api.V1;

namespace OmsAuthenticator.Tests.Clients
{
    public class OmsAuthenticatorClientLegacy : IOmsTokenClient, ISignatureClient
    {
        private readonly HttpClient _client;

        public OmsAuthenticatorClientLegacy(HttpClient client)
        {
            _client = client;
        }

        public async Task<TokenResponse> GetLastOmsToken(string omsId, string omsConnection)
        {
            var response = await _client.GetAsync($"/oms/token?registrationKey={omsConnection}&omsId={omsId}");
            return await GetOmsAuthenticatorResponse(response);
        }

        public async Task<TokenResponse> GetOmsToken(string omsId, string omsConnection, string requestId)
        {
            var response = await _client.PostAsync($"/oms/token",
                new StringContent(JsonSerializer.Serialize(new { omsId, omsConnection, requestId, registrationKey = omsConnection }), Encoding.UTF8, "application/json"));
            return await GetOmsAuthenticatorResponse(response);
        }

        private static async Task<TokenResponse> GetOmsAuthenticatorResponse(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponseV1>(responseContent)!;
            return new TokenResponse(response.StatusCode, tokenResponse.Token, tokenResponse.RequestId, tokenResponse.Errors);
        }

        public async Task<SignatureResponse> Sign(string value)
        {
            var response = await _client.PostAsync($"/api/v1/signature",
                new StringContent(JsonSerializer.Serialize(new { payloadBase64 = value }), Encoding.UTF8, "application/json"));
            return await GetSignatureResponse(response);
        }

        private async Task<SignatureResponse> GetSignatureResponse(HttpResponseMessage response)
        {
            var signatureResponse = JsonSerializer.Deserialize<SignatureResponseV1>(await response.Content.ReadAsStringAsync())!;
            return new SignatureResponse(response.StatusCode, signatureResponse.Signature, signatureResponse.Errors);
        }
    }
}
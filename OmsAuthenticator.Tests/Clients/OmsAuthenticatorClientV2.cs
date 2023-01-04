using System.Text;
using System.Text.Json;
using OmsAuthenticator.Api.V2;

namespace OmsAuthenticator.Tests.Clients
{
    public class OmsAuthenticatorClientV2 : IOmsTokenClient, ITrueTokenClient, ISignatureClient
    {
        private readonly HttpClient _client;
        private readonly string _providerKey;

        public OmsAuthenticatorClientV2(HttpClient client, string providerKey)
        {
            _client = client;
            _providerKey = providerKey;
        }

        public async Task<TokenResponse> GetLastOmsToken(string omsId, string omsConnection)
        {
            var response = await _client.GetAsync($"/api/v2/{_providerKey}/oms/token?omsid={omsId}&connectionid={omsConnection}");
            return await GetOmsAuthenticatorResponse(response);
        }

        public async Task<TokenResponse> GetOmsToken(string omsId, string omsConnection, string requestId)
        {
            var response = await _client.GetAsync($"/api/v2/{_providerKey}/oms/token?omsid={omsId}&connectionid={omsConnection}&requestid={requestId}");
            return await GetOmsAuthenticatorResponse(response);
        }

        public async Task<TokenResponse> GetTrueToken(string requestId)
        {
            var response = await _client.GetAsync($"/api/v2/{_providerKey}/true/token?requestid={requestId}");
            return await GetOmsAuthenticatorResponse(response);
        }

        public async Task<TokenResponse> GetLastTrueToken()
        {
            var response = await _client.GetAsync($"/api/v2/{_providerKey}/true/token");
            return await GetOmsAuthenticatorResponse(response);
        }

        public static async Task<TokenResponse> GetOmsAuthenticatorResponse(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = string.IsNullOrEmpty(responseContent)
                ? default
                : JsonSerializer.Deserialize<TokenResponseV2>(responseContent)!;
            return new TokenResponse(response.StatusCode, tokenResponse?.Token, tokenResponse?.RequestId, tokenResponse?.Errors);
        }

        public async Task<SignatureResponse> Sign(string value)
        {
            var response = await _client.PostAsync($"/api/v2/{_providerKey}/sign",
                new StringContent(JsonSerializer.Serialize(new { payloadBase64 = value }), Encoding.UTF8, "application/json"));
            return await GetSignatureResponse(response);
        }

        private async Task<SignatureResponse> GetSignatureResponse(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var signatureResponse = string.IsNullOrEmpty(responseContent)
                ? default
                : JsonSerializer.Deserialize<SignatureResponseV2>(responseContent);
            return new SignatureResponse(response.StatusCode, signatureResponse?.Signature, signatureResponse?.Errors);
        }
    }
}
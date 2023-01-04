using System.Text;
using System.Text.Json;
using OmsAuthenticator.Api.V1;

namespace OmsAuthenticator.Tests.Clients
{
    public class OmsAuthenticatorClientV1 : IOmsTokenClient
    {
        private readonly HttpClient _client;
        private readonly string _providerKey;

        public OmsAuthenticatorClientV1(HttpClient client, string providerKey)
        {
            _client = client;
            _providerKey = providerKey;
        }

        public async Task<TokenResponse> GetLastOmsToken(string omsId, string omsConnection)
        {
            var response = await _client.GetAsync($"/api/v1/{_providerKey}/oms/token?registrationKey={omsConnection}&omsId={omsId}");
            return await GetOmsAuthenticatorResponse(response);
        }

        public async Task<TokenResponse> GetOmsToken(string omsId, string omsConnection, string requestId)
        {
            var response = await _client.PostAsync($"/api/v1/{_providerKey}/oms/token",
                new StringContent(JsonSerializer.Serialize(new { omsId, omsConnection, requestId, registrationKey = omsConnection }), Encoding.UTF8, "application/json"));
            return await GetOmsAuthenticatorResponse(response);
        }

        public static async Task<TokenResponse> GetOmsAuthenticatorResponse(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = string.IsNullOrEmpty(responseContent)
                ? default
                : JsonSerializer.Deserialize<TokenResponseV1>(responseContent);
            return new TokenResponse(response.StatusCode, tokenResponse?.Token, tokenResponse?.RequestId, tokenResponse?.Errors);
        }
    }
}
using System.Text.Json.Serialization;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.ApiAdapters.DTABAC.V0
{
    public class DtabacTokenAdapter : IOmsTokenAdapter
    {
        public static readonly string HttpClientName = "dtabac";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Func<string, Token> _tokenFactory;

        public DtabacTokenAdapter(IHttpClientFactory httpClientFactory, Func<string, Token> tokenFactory)
        {
            _httpClientFactory = httpClientFactory;
            _tokenFactory = tokenFactory;
        }

        public async Task<Result<Token>> GetOmsTokenAsync(TokenKey tokenKey)
        {
            const string _url = "/gettoken";

            var httpClient = _httpClientFactory.CreateClient(HttpClientName);

            var result = await HttpResult.FromHttpResponseAsync<AuthTokenResponse>(
                async () => await httpClient.GetAsync($"{_url}?{tokenKey.ConnectionId}"));

            return result.Convert(ToAuthToken);

            Result<Token> ToAuthToken(AuthTokenResponse response) =>
                !string.IsNullOrEmpty(response.Token)
                    ? Result.Success(_tokenFactory(response.Token))
                    : Result.Failure<Token>($"{_url} returned empty token");
        }

        private class AuthTokenResponse
        {
            [JsonPropertyName("token")]
            public string? Token { get; set; }
        }
    }
}

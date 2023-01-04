using System.Text.Json.Serialization;
using OmsAuthenticator.Configuration;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.ApiAdapters.DTABAC.V0
{
    public class DtabacAdapterV0 : ITokenAdapter, IOmsTokenAdapter
    {
        public const string AdapterName = "dtabac-v0";
        private readonly TokenProviderConfig _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISystemTime _systemTime;

        public DtabacAdapterV0(TokenProviderConfig config, IHttpClientFactory httpClientFactory, ISystemTime systemTime)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _systemTime = systemTime;
        }

        public string PathSegment => _config.PathSegment;

        public async Task<Result<Token>> GetOmsTokenAsync(TokenKey.Oms omsTokenKey)
        {
            const string _url = "/gettoken";

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_config.Url);

            var result = await HttpResult.FromHttpResponseAsync<AuthTokenResponse>(
                async () => await httpClient.GetAsync($"{_url}?{omsTokenKey.ConnectionId}"));

            return result.Convert(ToAuthToken);

            Result<Token> ToAuthToken(AuthTokenResponse response) =>
                !string.IsNullOrEmpty(response.Token)
                    ? Result.Success(new Token(response.Token!, omsTokenKey.RequestId!, GetExpiration()))
                    : Result.Failure<Token>($"{_url}/gettoken returned empty token");
        }

        private DateTimeOffset GetExpiration() =>
            _systemTime.UtcNow.Add(_config.Expiration);

        public Task<Result<string>> SignAsync(string data)
        {
            throw new NotSupportedException();
        }

        private class AuthTokenResponse
        {
            [JsonPropertyName("token")]
            public string? Token { get; set; }
        }
    }
}

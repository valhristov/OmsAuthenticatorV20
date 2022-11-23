using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OmsAuthenticator.Configuration;
using OmsAuthenticator.Framework;
using OmsAuthenticator.Signing;

namespace OmsAuthenticator.ApiAdapters.GISMT.V3
{
    public class GisAdapterV3 : ITokenAdapter
    {
        public const string AdapterName = "oms-v3";

        private readonly TokenProviderConfig _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISystemTime _systemTime;
        private readonly ConsoleSignData _signData;

        public GisAdapterV3(TokenProviderConfig config, IHttpClientFactory httpClientFactory, ISystemTime systemTime, ConsoleSignData signData)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _systemTime = systemTime;
            _signData = signData;
        }

        public string PathSegment => _config.PathSegment;

        public async Task<Result<Token>> GetTokenAsync(TokenKey tokenKey)
        {
            if (tokenKey is TokenKey.Oms omsKey)
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(_config.Url);

                var authDataResult = await GetAuthData(httpClient);

                var signedDataResult = await authDataResult.ConvertAsync(async authData => await SignData(authData));

                var tokenResult = await signedDataResult.ConvertAsync(async authData => await GetOmsTokenAsync(omsKey, authData, httpClient));

                return tokenResult;
            }
            else
            {
                return Result.Failure<Token>($"Adapter '{AdapterName}' does not support tokens of type '{tokenKey.GetType().Name}'. Are you using the wrong URL?");
            }
        }

        public async Task<Result<string>> SignAsync(string data) =>
            await _signData.SignAsync(data, _config.Certificate);

        private async Task<Result<AuthData>> SignData(AuthData authData)
        {
            var result = await SignAsync(authData.Data);
            return result.Convert(value => Result.Success(new AuthData(authData.Uuid, value)));
        }

        private async Task<Result<AuthData>> GetAuthData(HttpClient httpClient)
        {
            // NOTE: we get auth data for both TRUE-API and OMS from the same URL.
            // The tokens are obtained from different paths and it is possible that
            // some day CRPT could decide to have different auth data paths too.
            const string url = "/api/v3/auth/cert/key";

            var result = await HttpResult.FromHttpResponseAsync<AuthDataResponse>(
                async () => await httpClient.GetAsync(url));

            return result.Convert(ToAuthData);

            static Result<AuthData> ToAuthData(AuthDataResponse response) =>
                response.Uuid != null && response.Data != null
                    ? Result.Success(new AuthData(response.Uuid, response.Data))
                    : Result.Failure<AuthData>("Response does not contain uuid or data");
        }

        private async Task<Result<Token>> GetOmsTokenAsync(TokenKey.Oms tokenKey, AuthData authData, HttpClient httpClient)
        {
            var url = $"/api/v3/auth/cert/{tokenKey.ConnectionId}";

            var tokenRequest = new GetTokenRequest
            {
                Data = authData.Data,
                Uuid = authData.Uuid,
            };

            var content = new StringContent(JsonSerializer.Serialize(tokenRequest), Encoding.UTF8, "application/json");

            var result = await HttpResult.FromHttpResponseAsync<AuthTokenResponse>(
                async () => await httpClient.PostAsync(url, content));

            return result.Convert(ToToken);

            Result<Token> ToToken(AuthTokenResponse response) =>
                response.Token != null && response.ErrorCode == null
                    ? Result.Success(new Token(response.Token, tokenKey.RequestId!, GetExpiration()))
                    : Result.Failure<Token>($"GIS-MT returned status OK, but no token. " +
                                            $"Error Code: '{response.ErrorCode}', " +
                                            $"Error Message: '{response.ErrorMessage}', " +
                                            $"Error Description: '{response.ErrorDescription}'");
        }

        private DateTimeOffset GetExpiration() =>
            _systemTime.UtcNow.Add(_config.Expiration);

        private record AuthData(string Uuid, string Data);

        private class AuthTokenResponse
        {
            [JsonPropertyName("token")]
            public string? Token { get; set; }
            [JsonPropertyName("code")]
            public string? ErrorCode { get; set; }
            [JsonPropertyName("error_message")]
            public string? ErrorMessage { get; set; }
            [JsonPropertyName("description")]
            public string? ErrorDescription { get; set; }
        }

        private class GetTokenRequest
        {
            [JsonPropertyName("uuid")]
            public string? Uuid { get; set; }
            [JsonPropertyName("data")]
            public string? Data { get; set; }
        }

        private class AuthDataResponse
        {
            [JsonPropertyName("uuid")]
            public string? Uuid { get; set; }
            [JsonPropertyName("data")]
            public string? Data { get; set; }
        }
    }
}

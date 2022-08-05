using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.ApiAdapters.GISMT.V3
{
    public class OmsTokenAdapter : IOmsTokenAdapter
    {
        public static readonly string HttpClientName = "gismt";

        private readonly IHttpClientFactory _httpClientFactory;

        public OmsTokenAdapter(IHttpClientFactory httpClientFactory, Func<string, Token> create)
        {
            _httpClientFactory = httpClientFactory;
        }

        private record AuthData(string Uuid, string Data);

        public async Task<Result<Token>> GetOmsTokenAsync(TokenKey tokenKey)
        {
            var httpClient = _httpClientFactory.CreateClient(HttpClientName);

            var authDataResult = await GetAuthData(httpClient);

            var signedDataResult = await authDataResult.ConvertAsync(async authData => await SignData(authData));

            var tokenResult = await signedDataResult.ConvertAsync(async authData => await GetToken(tokenKey.ConnectionId, authData, httpClient));

            return tokenResult;
        }

        // TODO
        private async Task<Result<AuthData>> SignData(AuthData authData) => Result.Success(authData);

        private async Task<Result<AuthData>> GetAuthData(HttpClient httpClient)
        {
            const string _url = "/api/v3/auth/cert/key";

            var result = await HttpResult.FromHttpResponseAsync<AuthDataResponse>(
                async () => await httpClient.GetAsync(_url));

            return result.Convert(ToAuthData);

            static Result<AuthData> ToAuthData(AuthDataResponse response) =>
                response.Uuid != null && response.Data != null
                    ? Result.Success(new AuthData(response.Uuid, response.Data))
                    : Result.Failure<AuthData>("Response does not contain uuid or data");
        }

        private async Task<Result<Token>> GetToken(string omsConnection, AuthData authData, HttpClient httpClient)
        {
            const string _url = "/api/v3/auth/cert";

            var content = new StringContent(JsonSerializer.Serialize(new GetTokenRequest { Data = authData.Data, Uuid = authData.Uuid }), Encoding.Unicode, "application/json");

            var result = await HttpResult.FromHttpResponseAsync<AuthTokenResponse>(
                async () => await httpClient.PostAsync($"{_url}/{omsConnection}", content));

            return result.Convert(ToToken);

            Result<Token> ToToken(AuthTokenResponse response) =>
                response.Token != null && response.ErrorCode == null
                    ? Result.Success(new Token(response.Token, DateTimeOffset.UtcNow.AddHours(10))) // TODO: 
                    : Result.Failure<Token>($"GIS-MT returned status OK, but no token. " +
                                            $"Error Code: '{response.ErrorCode}', " +
                                            $"Error Message: '{response.ErrorMessage}', " +
                                            $"Error Description: '{response.ErrorDescription}'");
        }

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

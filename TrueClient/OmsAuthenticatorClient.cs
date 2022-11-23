using System.Text.Json.Serialization;

namespace TrueClient
{
    public class OmsAuthenticatorClient
    {
        private readonly Uri _baseAddress;
        private readonly string _authenticatorAccount;

        public OmsAuthenticatorClient(Uri baseAddress, string authenticatorAccount)
        {
            _baseAddress = baseAddress;
            _authenticatorAccount = authenticatorAccount;
        }

        public async Task<Result<TrueApiToken>> GetTrueApiToken()
        {
            var httpClient = new HttpClient
            {
                BaseAddress = _baseAddress
            };

            var result = await HttpResult.FromHttpResponseAsync<TokenResponseV2>(async () => await httpClient.GetAsync($"/api/v2/{_authenticatorAccount}/true/token"));

            return result.Convert(response =>
                string.IsNullOrEmpty(response.Token)
                    ? Result.Failure<TrueApiToken>("Response did not contain token")
                    : Result.Success(new TrueApiToken(response.Token, response.Expires ?? DateTimeOffset.UtcNow.AddHours(10))));
        }

        public class TokenResponseV2
        {
            [JsonPropertyName("token"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonInclude]
            public string? Token { get; private set; }
            [JsonPropertyName("expires"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonInclude]
            public DateTimeOffset? Expires { get; private set; }
            [JsonPropertyName("requestId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonInclude]
            public string? RequestId { get; private set; }
            [JsonPropertyName("errors"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonInclude]
            public List<string>? Errors { get; private set; }
        }
    }
}

using System.Text.Json.Serialization;

namespace OmsAuthenticator.Api.V2
{
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

        // For testing
        public TokenResponseV2() { }

        public TokenResponseV2(IEnumerable<string> errors)
        {
            Errors = errors.ToList();
        }

        public TokenResponseV2(string? token, string requestId, DateTimeOffset expires)
        {
            Token = token;
            RequestId = requestId;
            Expires = expires;
        }
    }
}
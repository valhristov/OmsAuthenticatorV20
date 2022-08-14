using System.Text.Json.Serialization;

namespace OmsAuthenticator.Api.V1
{
    public class TokenResponseV1
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
        public TokenResponseV1() { }

        public TokenResponseV1(IEnumerable<string> errors)
        {
            Errors = errors.ToList();
        }

        public TokenResponseV1(string? token, string requestId, DateTimeOffset expires)
        {
            Token = token;
            RequestId = requestId;
            Expires = expires;
        }
    }
}
using System.Text.Json.Serialization;

namespace OmsAuthenticator.Api.V2
{
    public class SignatureResponse
    {
        [JsonPropertyName("signature"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonInclude]
        public string? Signature { get; private set; }
        [JsonPropertyName("errors"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonInclude]
        public List<string>? Errors { get; private set; }

        // For testing
        public SignatureResponse() { }

        public SignatureResponse(IEnumerable<string> errors)
        {
            Errors = errors.ToList();
        }

        public SignatureResponse(string signature)
        {
            Signature = signature;
        }
    }
}

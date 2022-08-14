using System.Text.Json.Serialization;

namespace OmsAuthenticator.Api.V1
{
    public class SignatureResponse
    {
        [JsonPropertyName("signature"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Signature { get; }
        [JsonPropertyName("errors"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<string>? Errors { get; }

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

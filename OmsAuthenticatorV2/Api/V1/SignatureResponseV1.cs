using System.Text.Json.Serialization;

namespace OmsAuthenticator.Api.V1
{
    public class SignatureResponseV1
    {
        [JsonPropertyName("signature"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonInclude]
        public string? Signature { get; private set; }
        [JsonPropertyName("errors"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonInclude]
        public List<string>? Errors { get; private set; }

        // For testing
        public SignatureResponseV1() { }

        public SignatureResponseV1(IEnumerable<string> errors)
        {
            Errors = errors.ToList();
        }

        public SignatureResponseV1(string signature)
        {
            Signature = signature;
        }
    }
}

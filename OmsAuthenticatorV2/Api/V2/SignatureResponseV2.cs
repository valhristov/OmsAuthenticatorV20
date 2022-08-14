using System.Text.Json.Serialization;

namespace OmsAuthenticator.Api.V2
{
    public class SignatureResponseV2
    {
        [JsonPropertyName("signature"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonInclude]
        public string? Signature { get; private set; }
        [JsonPropertyName("errors"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonInclude]
        public List<string>? Errors { get; private set; }

        // For testing
        public SignatureResponseV2() { }

        public SignatureResponseV2(IEnumerable<string> errors)
        {
            Errors = errors.ToList();
        }

        public SignatureResponseV2(string signature)
        {
            Signature = signature;
        }
    }
}

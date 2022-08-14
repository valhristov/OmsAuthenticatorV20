using System.Text.Json.Serialization;

namespace OmsAuthenticator.Api.V2
{
    public class SignatureRequestV2
    {
        /// <summary>
        /// Base64-encoded string to sign.
        /// </summary>
        [JsonPropertyName("payloadBase64")]
        public string? PayloadBase64 { get; set; }
    }
}

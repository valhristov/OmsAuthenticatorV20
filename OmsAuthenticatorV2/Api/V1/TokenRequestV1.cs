using System.Text.Json.Serialization;

namespace OmsAuthenticator.Api.V1
{
    public class TokenRequestV1
    {
        [JsonPropertyName("omsId")]
        public string? OmsId { get; set; }
        [JsonPropertyName("omsConnection")]
        public string? OmsConnection { get; set; }
        [JsonPropertyName("registrationKey")]
        public string? RegistrationKey { get; set; }
        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }
    }
}

﻿using System.Text.Json.Serialization;

namespace OmsAuthenticator.Api.V1
{
    public class TokenRequest
    {
        [JsonPropertyName("omsId")]
        public string? OmsId { get; set; }
        [JsonPropertyName("omsConnection")]
        public string? OmsConnection { get; set; }
        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }
    }
}
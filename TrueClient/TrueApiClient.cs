using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrueClient
{
    public class TrueApiClient
    {
        private readonly HttpClient _httpClient;

        public TrueApiClient(Uri baseAddress)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = baseAddress,
            };
        }

        public async Task<Result<ImmutableArray<CodeInfo>>> GetCodesInformation(TrueApiToken token, params string[] codes)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

            var content = new StringContent(JsonSerializer.Serialize(codes), Encoding.UTF8, "application/json");

            var result = await HttpResult.FromHttpResponseAsync<CisInfoResponse[]>(
                async () => await _httpClient.PostAsync("/api/v3/true-api/cises/info", content),
                response =>
                    response.IsSuccessStatusCode ||
                    response.StatusCode == System.Net.HttpStatusCode.NotFound); // The API returns 404 when it cannot find all of the codes

            return result.Convert(cisInfoResponses =>
                Result.Success(cisInfoResponses.Select(CreateCodeInfo).ToImmutableArray()));

            CodeInfo CreateCodeInfo(CisInfoResponse response)
            {
                return new CodeInfo(
                    response.CisInfo?.Cis ?? response.CisInfo?.RequestedCis ?? string.Empty,
                    GetCodeStatus(response),
                    response.ErrorMessage ?? string.Empty
                    );

                CodeStatus GetCodeStatus(CisInfoResponse response) =>
                    response switch
                    {
                        { ErrorMessage: not null } or
                        { ErrorCode: not null } or
                        { CisInfo: null } => CodeStatus.NOT_FOUND,
                        { CisInfo: { Status: "EMITTED" } } => CodeStatus.EMITTED,
                        { CisInfo: { Status: "APPLIED" } } => CodeStatus.APPLIED,
                        { CisInfo: { Status: "INTRODUCED" } } => CodeStatus.INTRODUCED,
                        { CisInfo: { Status: "WRITTEN_OFF" } } => CodeStatus.WRITTEN_OFF,
                        { CisInfo: { Status: "RETIRED" } } => CodeStatus.RETIRED,
                        { CisInfo: { Status: "WITHDRAWN" } } => CodeStatus.WITHDRAWN,
                        { CisInfo: { Status: "DISAGGREGATION" } } => CodeStatus.DISAGGREGATION,
                        { CisInfo: { Status: "DISAGGREGATED" } } => CodeStatus.DISAGGREGATED,
                        { CisInfo: { Status: "APPLIED_NOT_PAID" } } => CodeStatus.APPLIED_NOT_PAID,
                        _ => throw new NotImplementedException("Unknown code status"),
                    };
            }
        }

        private class CisInfo
        {
            [JsonPropertyName("applicationDate")]
            public DateTimeOffset? ApplicationDate { get; set; }
            [JsonPropertyName("requestedCis")]
            public string? RequestedCis { get; set; }
            [JsonPropertyName("cis")]
            public string? Cis { get; set; }
            [JsonPropertyName("gtin")]
            public string? Gtin { get; set; }
            [JsonPropertyName("tnVedEaes")]
            public string? TnVedEaes { get; set; }
            [JsonPropertyName("tnVedEaesGroup")]
            public string? TnVedEaesGroup { get; set; }
            [JsonPropertyName("productName")]
            public string? ProductName { get; set; }
            [JsonPropertyName("productGroupId")]
            public int? ProductGroupId { get; set; }
            [JsonPropertyName("productGroup")]
            public string? ProductGroup { get; set; }
            [JsonPropertyName("brand")]
            public string? Brand { get; set; }
            [JsonPropertyName("producedDate")]
            public DateTimeOffset? ProducedDate { get; set; }
            [JsonPropertyName("emissionDate")]
            public DateTimeOffset? EmissionDate { get; set; }
            [JsonPropertyName("emissionType")]
            public string? EmissionType { get; set; }
            [JsonPropertyName("packageType")]
            public string? PackageType { get; set; }
            [JsonPropertyName("generalPackageType")]
            public string? GeneralPackageType { get; set; }
            [JsonPropertyName("ownerInn")]
            public string? OwnerInn { get; set; }
            [JsonPropertyName("ownerName")]
            public string? OwnerName { get; set; }
            [JsonPropertyName("status")]
            public string? Status { get; set; }
            [JsonPropertyName("statusEx")]
            public string? StatusEx { get; set; }
            [JsonPropertyName("child")]
            public string[]? Child { get; set; }
            [JsonPropertyName("producerInn")]
            public string? ProducerInn { get; set; }
            [JsonPropertyName("producerName")]
            public string? ProducerName { get; set; }
            [JsonPropertyName("markWithdraw")]
            public bool? MarkWithdraw { get; set; }
            [JsonPropertyName("maxRetailPrice")]
            public int? MaxRetailPrice { get; set; }
        }

        private class CisInfoResponse
        {
            [JsonPropertyName("cisInfo")]
            public CisInfo? CisInfo { get; set; }
            [JsonPropertyName("errorMessage")]
            public string? ErrorMessage { get; set; }
            [JsonPropertyName("errorCode")]
            public string? ErrorCode { get; set; }
        }
    }
}

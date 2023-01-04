using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace OmsAuthenticator.Tests.Helpers
{
    public class TrueApi
    {
        private readonly HttpHelper _httpHelper;

        public TrueApi(HttpHelper httpHelper)
        {
            _httpHelper = httpHelper;
        }

        public void SetupTokenSuccess()
        {
            var data = Guid.NewGuid().ToString();
            _httpHelper.Add(GetCertKeySuccess(data));
            _httpHelper.Add(GetTokenSuccess(data));
        }

        public void SetupCertKeyFailure()
        {
            _httpHelper.Add(GetCertKeyFailure(HttpStatusCode.BadRequest, "boom"));
        }

        public void SetupTokenFailure()
        {
            var data = Guid.NewGuid().ToString().Replace("-", "");
            _httpHelper.Add(GetCertKeySuccess(data));
            _httpHelper.Add(GetTokenFailure(data, HttpStatusCode.BadRequest, "boom"));
        }

        public IEnumerable<string> IssuedTokens
        {
            get
            {
                return _httpHelper.Responses
                    .Where(x => x.StatusCode == HttpStatusCode.OK && IsGetToken(x.Request.PathAndQuery))
                    .Select(GetToken);

                bool IsGetToken(string pathAndQuery) =>
                    pathAndQuery.StartsWith("/api/v3/true-api/auth/simpleSignIn");

                string GetToken(SentResponse response) =>
                    JsonSerializer.Deserialize<AuthTokenResponse>(response.JsonContent!)!.Token!;
            }
        }

        private static RequestMatcher GetCertKeySuccess(string data) =>
                new RequestMatcher(HttpMethod.Get, "/api/v3/auth/cert/key", "",
                    new ResponseTemplate(HttpStatusCode.OK,
                        req => JsonSerializer.Serialize(new { uuid = Guid.NewGuid().ToString(), data, })));

        private static RequestMatcher GetCertKeyFailure(HttpStatusCode statusCode, string content) =>
            new RequestMatcher(HttpMethod.Get, "/api/v3/auth/cert/key", "",
                new ResponseTemplate(statusCode, req => content));

        private static RequestMatcher GetTokenSuccess(string data)
        {
            data.Should().NotStartWith("signed:", because: "'signed:' will be added automatically.");
            return new RequestMatcher(HttpMethod.Post, "/api/v3/true-api/auth/simpleSignIn", "signed:" + data,
                new ResponseTemplate(HttpStatusCode.OK,
                    req => JsonSerializer.Serialize(new { token = Guid.NewGuid().ToString(), })));
        }

        private static RequestMatcher GetTokenFailure(string data, HttpStatusCode statusCode, string content)
        {
            data.Should().NotStartWith("signed:", because: "'signed:' will be added automatically.");
            return new RequestMatcher(HttpMethod.Post, "/api/v3/true-api/auth/simpleSignIn", "signed:" + data,
                new ResponseTemplate(statusCode, req => content));
        }

        public static void IsGetCertKey(ReceivedRequest request) =>
            request.PathAndQuery.Should().Be("/api/v3/auth/cert/key");

        public static void IsGetToken(ReceivedRequest request) =>
            request.PathAndQuery.Should().StartWith($"/api/v3/true-api/auth/simpleSignIn");

        public void GetTokenExecuted(int times)
        {
            _httpHelper.Requests.Should().HaveCount(times * 2); // each token is 2 requests
            for (int i = 0; i < times; i++)
            {
                IsGetCertKey(_httpHelper.Requests.ElementAt(i * 2));
                IsGetToken(_httpHelper.Requests.ElementAt(i * 2 + 1));
            }
        }

        public void FirstStepExecuted(int times)
        {
            _httpHelper.Requests.Where(IsGetCertKey).Should().HaveCount(times);

            static bool IsGetCertKey(ReceivedRequest request) =>
                request.PathAndQuery == "/api/v3/auth/cert/key";
        }

        private class AuthTokenResponse
        {
            [JsonPropertyName("token")]
            public string? Token { get; set; }
        }
    }

}
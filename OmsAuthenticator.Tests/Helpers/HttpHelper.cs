using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Moq;
using Moq.Protected;

namespace OmsAuthenticator.Tests.Helpers
{
    public record SentResponse(HttpStatusCode StatusCode, string? JsonContent, ReceivedRequest Request);

    public record ReceivedRequest(string PathAndQuery, HttpMethod Method, string? JsonBody);

    public record ResponseTemplate(HttpStatusCode StatusCode, Func<ReceivedRequest, string> GetResponseContent);

    public record RequestMatcher(HttpMethod Method, string PathOrRegex, string? BodyOrRegex, ResponseTemplate Response);

    public class HttpHelper
    {
        private readonly List<RequestMatcher> _requestMatchers = new List<RequestMatcher>();

        public List<ReceivedRequest> Requests { get; } = new List<ReceivedRequest>();
        public List<SentResponse> Responses { get; } = new List<SentResponse>();

        public HttpMessageHandler MessageHandler { get; }

        public HttpHelper()
        {
            var messageHandlerMock = new Mock<HttpMessageHandler>();
            messageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (requestMessage, token) =>
                {
                    var receivedRequest = await CreateReceivedRequestAsync(requestMessage);
                    Requests.Add(receivedRequest);

                    var requestTemplates = FindMatchingRequestTemplates(receivedRequest);

                    var responseToSend = requestTemplates.Length == 0
                        ? throw new KeyNotFoundException($"Cannot find request template that matches '{receivedRequest.Method} {receivedRequest.PathAndQuery}'")
                        : CreateResponse(receivedRequest, requestTemplates[0].Response);

                    Responses.Add(responseToSend);

                    return new HttpResponseMessage(responseToSend.StatusCode)
                    {
                        Content = new StringContent(responseToSend.JsonContent ?? ""),
                        RequestMessage = requestMessage,
                    };
                });

            MessageHandler = messageHandlerMock.Object;
        }

        public void Add(RequestMatcher matcher) =>
            _requestMatchers.Add(matcher);

        public void AddRange(IEnumerable<RequestMatcher> matchers) =>
            _requestMatchers.AddRange(matchers);

        public void RemoveAll(Predicate<RequestMatcher> predicate) =>
            _requestMatchers.RemoveAll(predicate);

        private static async Task<ReceivedRequest> CreateReceivedRequestAsync(HttpRequestMessage requestMessage)
        {
            var requestBody = requestMessage.Content is HttpContent content
                ? await content.ReadAsStringAsync()
                : "";
            return new ReceivedRequest(requestMessage.RequestUri?.PathAndQuery ?? "", requestMessage.Method, requestBody);
        }

        private SentResponse CreateResponse(ReceivedRequest request, ResponseTemplate template) =>
            new SentResponse(template.StatusCode, template.GetResponseContent(request), request);

        private RequestMatcher[] FindMatchingRequestTemplates(ReceivedRequest request)
        {
            return _requestMatchers
                .Where(template => template.Method == request.Method)
                .Where(template =>
                    request.PathAndQuery.StartsWith(template.PathOrRegex) ||
                    new Regex(template.PathOrRegex).IsMatch(request.PathAndQuery))
                .ToArray();
        }
    }
}
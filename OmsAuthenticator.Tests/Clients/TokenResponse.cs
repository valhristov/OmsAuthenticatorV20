using System.Net;
using FluentAssertions;

namespace OmsAuthenticator.Tests.Clients
{
    public record TokenResponse(HttpStatusCode StatusCode, string? Token, string? RequestId, List<string>? Errors)
    {
        public void ShouldBeOk(string token, string requestId)
        {
            StatusCode.Should().Be(HttpStatusCode.OK);
            Token.Should().Be(token);
            RequestId.Should().Be(requestId);
            Errors.Should().BeNull();
        }

        public void ShouldBeUnprocessableEntity(string errorMessage)
        {
            StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            Token.Should().BeNull();
            RequestId.Should().BeNull();
            Errors.Should().BeEquivalentTo(errorMessage);
        }

        public void ShouldBeNotFound()
        {
            StatusCode.Should().Be(HttpStatusCode.NotFound);
            Token.Should().BeNull();
            RequestId.Should().BeNull();
        }

        public void ShouldBeBadRequest(string errorMessage)
        {
            StatusCode.Should().Be(HttpStatusCode.BadRequest);
            Token.Should().BeNull();
            RequestId.Should().BeNull();
            Errors.Should().BeEquivalentTo(errorMessage);
        }
    }
}
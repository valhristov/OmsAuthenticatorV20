using System.Net;
using FluentAssertions;

namespace OmsAuthenticator.Tests.Clients
{
    public record SignatureResponse(HttpStatusCode StatusCode, string? SignedValue, List<string>? Errors)
    {
        public void ShouldBeOk(string expectedSignedValue)
        {
            StatusCode.Should().Be(HttpStatusCode.OK);
            SignedValue.Should().Be(expectedSignedValue);
            Errors.Should().BeNullOrEmpty();
        }

        public void ShouldBeUnprocessableEntity(string expectedError)
        {
            StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            SignedValue.Should().BeNull();
            Errors.Should().BeEquivalentTo(expectedError);
        }
    }
}
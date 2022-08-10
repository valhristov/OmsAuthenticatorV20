namespace OmsAuthenticator
{
    public record Token(string Value, string RequestId, DateTimeOffset Expires);
}

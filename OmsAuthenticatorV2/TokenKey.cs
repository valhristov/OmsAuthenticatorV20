namespace OmsAuthenticator
{
    public abstract record TokenKey(string? RequestId)
    {
        public record Oms(string ApplicationId, string OmsId, string ConnectionId, string? RequestId) : TokenKey(RequestId);

        public record TrueApi(string? RequestId) : TokenKey(RequestId);
    }
}

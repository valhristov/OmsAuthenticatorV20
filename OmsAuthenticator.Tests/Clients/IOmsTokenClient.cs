namespace OmsAuthenticator.Tests.Clients
{
    public interface IOmsTokenClient
    {
        Task<TokenResponse> GetLastOmsToken(string omsId, string omsConnection);
        Task<TokenResponse> GetOmsToken(string omsId, string omsConnection, string requestId);
    }
}
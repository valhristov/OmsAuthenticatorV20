namespace OmsAuthenticator.Tests.Clients
{
    public interface ITrueTokenClient
    {
        Task<TokenResponse> GetTrueToken(string requestId);
        Task<TokenResponse> GetLastTrueToken();
    }
}
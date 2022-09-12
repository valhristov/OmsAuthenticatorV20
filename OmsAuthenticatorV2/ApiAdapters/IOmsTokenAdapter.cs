using OmsAuthenticator.Framework;

namespace OmsAuthenticator.ApiAdapters
{
    public interface IOmsTokenAdapter
    {
        string PathSegment { get; }

        Task<Result<Token>> GetTokenAsync(TokenKey tokenKey);
        Task<Result<string>> SignAsync(string data);
    }
}

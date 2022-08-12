using OmsAuthenticator.Framework;

namespace OmsAuthenticator.ApiAdapters
{
    public interface IOmsTokenAdapter
    {
        string PathSegment { get; }

        // TODO: add validate token key method to enforce adapter-specific requirements (i.e. connection id to be required)

        Task<Result<Token>> GetOmsTokenAsync(TokenKey tokenKey);
    }
}

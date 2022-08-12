using OmsAuthenticator.Framework;

namespace OmsAuthenticator.ApiAdapters
{
    public interface IOmsTokenAdapter
    {
        string PathSegment { get; }

        Task<Result<Token>> GetOmsTokenAsync(TokenKey.Oms tokenKey);
        Task<Result<Token>> GetTrueTokenAsync(TokenKey.TrueApi tokenKey);
    }
}

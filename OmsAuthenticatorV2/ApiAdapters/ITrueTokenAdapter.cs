using OmsAuthenticator.Framework;

namespace OmsAuthenticator.ApiAdapters;

public interface ITrueTokenAdapter
{
    Task<Result<Token>> GetTrueTokenAsync(TokenKey.TrueApi key);
}

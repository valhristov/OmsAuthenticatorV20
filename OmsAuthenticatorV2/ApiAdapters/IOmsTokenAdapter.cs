using OmsAuthenticator.Framework;

namespace OmsAuthenticator.ApiAdapters;

public interface IOmsTokenAdapter
{
    Task<Result<Token>> GetOmsTokenAsync(TokenKey.Oms key);
}

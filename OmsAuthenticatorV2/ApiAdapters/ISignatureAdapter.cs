using OmsAuthenticator.Framework;

namespace OmsAuthenticator.ApiAdapters;

public interface ISignatureAdapter
{
    Task<Result<string>> SignAsync(string value);
}

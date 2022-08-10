using System.Collections.Immutable;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Configuration;

public record AuthenticatorConfig(ImmutableArray<TokenProviderConfig> TokenProviders, SignerConfig Signer)
{
    public static Result<AuthenticatorConfig> Get(IConfiguration parent)
    {
        var section = parent.GetSection("Authenticator");
        if (!section.Exists())
        {
            return Result.Failure<AuthenticatorConfig>(
                "Cannot find required section 'Authenticator' in appSettings.json or it is empty.");
        }

        var tokenProvidersConfig = TokenProviderConfig.CreateMany(section);
        
        var signerConfig = SignerConfig.Create(section);

        return Result.Combine(tokenProvidersConfig, signerConfig).Convert(x =>
            Result.Success(new AuthenticatorConfig(x.Item1, x.Item2))); 
    }
}
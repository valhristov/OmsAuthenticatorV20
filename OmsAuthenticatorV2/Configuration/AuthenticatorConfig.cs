using System.Collections.Immutable;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Configuration;

public record AuthenticatorConfig(ImmutableArray<TokenProviderConfig> TokenProviders, SignerConfig Signer)
{
    public static Result<AuthenticatorConfig> Create(IConfiguration root)
    {
        return root.GetRequiredSection("Authenticator")
            .Convert(authenticatorSection =>
                Result
                    .Combine(TokenProviderConfig.CreateMany(authenticatorSection), SignerConfig.Create(authenticatorSection))
                    .Convert(tuple => Result.Success(new AuthenticatorConfig(tuple.Item1, tuple.Item2))));
    }
}
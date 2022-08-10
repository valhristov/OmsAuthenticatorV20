using System.Collections.Immutable;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Configuration;

public record TokenProviderConfig(string Key, string Adapter, string Url, string Certificate, TimeSpan Expiration)
{
    public static Result<ImmutableArray<TokenProviderConfig>> CreateMany(IConfigurationSection parent)
    {
        var section = parent.GetSection("TokenProviders");
        if (!section.Exists() || !section.GetChildren().Any())
        {
            return Result.Failure<ImmutableArray<TokenProviderConfig>>(
                "Cannot find required section 'Authenticator:TokenProviders' in appSettings.json or it is empty.");
        }

        var results = section.GetChildren().Select(Create).ToArray();
        return Result.Combine(results) // return Failure if any of the results is Failure
            .Convert(x => Result.Success(x.ToImmutableArray()));
    }

    private static Result<TokenProviderConfig> Create(IConfigurationSection parent)
    {
        if (parent.GetValue<string>("Adapter") is null)
        {
            return Result.Failure<TokenProviderConfig>(
                $"Cannot find required section '{parent.Path}:Adapter' in appSettings.json or it is empty.");
        }
        if (parent.GetValue<string>("Url") is null)
        {
            return Result.Failure<TokenProviderConfig>(
                $"Cannot find required section '{parent.Path}:Url' in appSettings.json or it is empty.");
        }
        return Result.Success(
            new TokenProviderConfig(
                parent.Key,
                parent["Adapter"],
                parent["Url"],
                parent["Certificate"],
                parent.GetValue<TimeSpan>("Expiration")));
    }
}

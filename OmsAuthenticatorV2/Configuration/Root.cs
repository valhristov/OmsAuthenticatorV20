using System.Collections.Immutable;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Configuration;

public record TokenProviderConfig(string Key, string Adapter, string Url, string Certificate, TimeSpan Expiration)
{
    public static Result<TokenProviderConfig> Create(IConfigurationSection parent)
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

public record TokenProvidersConfig(ImmutableArray<TokenProviderConfig> TokenProviders)
{
    public static Result<TokenProvidersConfig> Create(IConfigurationSection parent)
    {
        var section = parent.GetSection("TokenProviders");
        if (!section.Exists() || !section.GetChildren().Any())
        {
            return Result.Failure<TokenProvidersConfig>(
                "Cannot find required section 'Authenticator:TokenProviders' in appSettings.json or it is empty.");
        }

        var results = section.GetChildren().Select(TokenProviderConfig.Create).ToArray();
        return Result.Combine(results) // return Failure if any of the results is Failure
            .Convert(x => Result.Success(new TokenProvidersConfig(x.ToImmutableArray())));
    }
}

public record SignerConfig(string Path)
{
    public static Result<SignerConfig> Create(IConfigurationSection section)
    {
        var path = section.GetValue<string>("SignDataPath");
        return path == null
            ? Result.Failure<SignerConfig>($"Cannot find required configuration element '{section.Path}:SignDataPath'.")
            : Result.Success(new SignerConfig(path));
    }
}

public record AuthenticatorConfig(TokenProvidersConfig TokenProviders, SignerConfig Signer)
{
    public static Result<AuthenticatorConfig> Get(IConfiguration parent)
    {
        var section = parent.GetSection("Authenticator");
        if (!section.Exists())
        {
            return Result.Failure<AuthenticatorConfig>(
                "Cannot find required section 'Authenticator' in appSettings.json or it is empty.");
        }

        var tokenProvidersConfig = TokenProvidersConfig.Create(section);
        
        var signerConfig = SignerConfig.Create(section);

        return Result.Combine(tokenProvidersConfig, signerConfig).Convert(x =>
            Result.Success(new AuthenticatorConfig(x.Item1, x.Item2))); 
    }
}
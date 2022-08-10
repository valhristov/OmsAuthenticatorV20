using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Configuration;

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

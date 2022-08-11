using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Configuration;

public record SignerConfig(string Path)
{
    public static Result<SignerConfig> Create(IConfigurationSection section) => 
        section
            .GetRequiredValue("SignDataPath")
            .Convert(path =>
                File.Exists(path) || path == "integration tests"
                ? Result.Success(new SignerConfig(path))
                : Result.Failure<SignerConfig>($"Cannot find '{path}'"));
}

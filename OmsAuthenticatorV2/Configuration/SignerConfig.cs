using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Configuration;

public record SignerConfig(string Path)
{
    public static Result<SignerConfig> Create(IConfigurationSection section) => 
        section
            .GetRequiredValue("SignDataPath")
            .Convert(path => Result.Success(System.IO.Path.GetFullPath(path)))
            .Convert(fullPath =>
                File.Exists(fullPath)
                    ? Result.Success(new SignerConfig(fullPath))
                    : Result.Failure<SignerConfig>($"Cannot find '{fullPath}'"));
}

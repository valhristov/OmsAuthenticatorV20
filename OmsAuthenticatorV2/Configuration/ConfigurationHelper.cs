using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Configuration
{
    public static class ConfigurationHelper
    {
        public static Result<IConfigurationSection> GetRequiredSection(this IConfiguration parent, string name)
        {
            var section = parent.GetSection(name);
            return section.Exists() && section.GetChildren().Any()
                ? Result.Success(section)
                : Result.Failure<IConfigurationSection>($"Cannot find required section '{section.Path}' or it is empty.");
        }

        public static Result<string> GetRequiredValue(this IConfigurationSection section, string name)
        {
            var value = section[name];
            return value != null
                ? Result.Success(value)
                : Result.Failure<string>($"Cannot find required value '{section.Path}:{name}'.");
        }
    }
}

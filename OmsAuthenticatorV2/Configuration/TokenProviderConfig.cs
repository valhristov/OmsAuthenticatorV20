using System.Collections.Immutable;
using OmsAuthenticator.ApiAdapters.DTABAC.V0;
using OmsAuthenticator.ApiAdapters.GISMT.V3;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Configuration;

public record TokenProviderConfig(string PathSegment, string AdapterName, string Url, string Certificate, TimeSpan Expiration)
{
    public static Result<ImmutableArray<TokenProviderConfig>> CreateMany(IConfigurationSection parent) =>
        parent.GetRequiredSection("TokenProviders")
            .Convert(section =>
                Result
                    .Combine(section.GetChildren().Select(Create)) // return Failure if any of the results is Failure
                    .Convert(x => Result.Success(x.ToImmutableArray())));

    private static Result<TokenProviderConfig> Create(IConfigurationSection parent) =>
        Result
            .Combine(new[]
            {
                parent.GetRequiredValue("Adapter").Convert(ValidateAdapter),
                parent.GetRequiredValue("Url"),
                parent.GetRequiredValue("Expiration").Convert(ValidateExpiration),
            })
            .Convert(values => // Values are validated at this point, in the same order as above
                Result.Success(new TokenProviderConfig(
                    parent.Key,
                    values.ElementAt(0),
                    values.ElementAt(1),
                    parent["Certificate"],
                    TimeSpan.Parse(values.ElementAt(2)))));

    private static Result<string> ValidateExpiration(string expiration) =>
        TimeSpan.TryParse(expiration, out _)
            ? Result.Success(expiration)
            : Result.Failure<string>($"Configured Expiration '{expiration}' is not a valid time span.");

    private static Result<string> ValidateAdapter(string adapter) =>
        adapter switch
        {
            GisAdapterV3.AdapterName or
            DtabacAdapterV0.AdapterName => Result.Success(adapter),
            _ => Result.Failure<string>($"Configured Adapter '{adapter}' is not supported."),
        };
}

using System.Collections.Immutable;
using OmsAuthenticator;
using OmsAuthenticator.Api.V2;
using OmsAuthenticator.ApiAdapters.GISMT.V3;
using OmsAuthenticator.Configuration;
using OmsAuthenticator.Framework;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ISystemTime>(new SystemTime());

builder.Host.UseWindowsService(); // TODO: fix this

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) { }

app.UseHttpsRedirection();

var systemTime = app.Services.GetRequiredService<ISystemTime>();
var cache = new AsyncTokenResultCache(() => systemTime.UtcNow);
var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();

var configuration = app.Services.GetRequiredService<IConfiguration>();

var configResult = AuthenticatorConfig.Get(configuration)
    .Convert(GetValidConfiguration);

var messages = configResult.Select(
    StartApplication,
    errors => errors);

foreach (var message in messages)
{
    Console.WriteLine(message);
}

Result<AuthenticatorConfig> GetValidConfiguration(AuthenticatorConfig config)
{
    var notSupportedAdapters = config.TokenProviders.TokenProviders.Where(x => x.Adapter != GisAdapterV3.AdapterName);
    if (notSupportedAdapters.Any())
    {
        return Result.Failure<AuthenticatorConfig>("The configured token providers are not supported: " + string.Join(", ", notSupportedAdapters.Select(x => x.Key)));
    }
    return Result.Success(config);
}

ImmutableArray<string> StartApplication(AuthenticatorConfig config)
{
    foreach (var tokenProviderConfig in config.TokenProviders.TokenProviders)
    {
        var controller = tokenProviderConfig.Adapter switch
        {
            GisAdapterV3.AdapterName => new TokenControllerV2(cache, new GisAdapterV3(
                    () =>
                    {
                        var client = httpClientFactory.CreateClient();
                        client.BaseAddress = new Uri(tokenProviderConfig.Url);
                        return client;
                    },
                    () => systemTime.UtcNow.Add(tokenProviderConfig.Expiration))),
            _ => throw new NotSupportedException(""),
        };

        app.MapGet($"/api/v2/{tokenProviderConfig.Key}/oms/token/", controller.GetOmsTokenAsync);
        // app.MapGet($"/api/v2/token/true/{tokenProviderConfig.Key}", controller.GetTrueTokenAsync);
    }
    
    app.Run();
    
    return ImmutableArray.Create("Successfully stopped the application.");
}


// Make the implicit Program class public so test projects can access it
public partial class Program { }

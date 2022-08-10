using System.Collections.Immutable;
using OmsAuthenticator.Api.V2;
using OmsAuthenticator.ApiAdapters;
using OmsAuthenticator.ApiAdapters.GISMT.V3;
using OmsAuthenticator.Configuration;
using OmsAuthenticator.Framework;

var builder = WebApplication.CreateBuilder(
    new WebApplicationOptions
    {
        ContentRootPath = AppContext.BaseDirectory, // needed for builder.Host.UseWindowsService() 
        Args = args,
        ApplicationName = System.Diagnostics.Process.GetCurrentProcess().ProcessName,
    });

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ISystemTime>(new SystemTime());

builder.Host.UseWindowsService();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) { }

app.UseHttpsRedirection();

var systemTime = app.Services.GetRequiredService<ISystemTime>();
var cache = new AsyncTokenResultCache(() => systemTime.UtcNow);
var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();

var configuration = app.Services.GetRequiredService<IConfiguration>();

var messages = AuthenticatorConfig.Get(configuration)
    .Convert(GetValidConfiguration)
    .Convert(GetAdapterInstances)
    .Select(StartApplication, errors => errors);

foreach (var message in messages)
{
    Console.WriteLine(message);
}

Result<ImmutableArray<(TokenProviderConfig, IOmsTokenAdapter)>> GetAdapterInstances(AuthenticatorConfig config) =>
    Result.Success(config.TokenProviders.Select(x =>
    {
        var adapter = x.Adapter switch
        {
            GisAdapterV3.AdapterName => new GisAdapterV3(
                    () =>
                    {
                        var client = httpClientFactory.CreateClient();
                        client.BaseAddress = new Uri(x.Url);
                        return client;
                    },
                    () => systemTime.UtcNow.Add(x.Expiration)),
            _ => throw new NotSupportedException(""),
        };
        return (x, (IOmsTokenAdapter)adapter);
    }).ToImmutableArray());

Result<AuthenticatorConfig> GetValidConfiguration(AuthenticatorConfig config)
{
    var notSupportedAdapters = config.TokenProviders.Where(x => x.Adapter != GisAdapterV3.AdapterName);
    if (notSupportedAdapters.Any())
    {
        return Result.Failure<AuthenticatorConfig>("The configured token providers are not supported: " + string.Join(", ", notSupportedAdapters.Select(x => x.Key)));
    }
    return Result.Success(config);
}

ImmutableArray<string> StartApplication(ImmutableArray<(TokenProviderConfig Config, IOmsTokenAdapter Instance)> adapters)
{
    foreach (var adapter in adapters)
    {
        app.MapGet($"/api/v2/{adapter.Config.Key}/oms/token/", new TokenControllerV2(cache, adapter.Instance).GetOmsTokenAsync);
    }

    app.Run();

    return ImmutableArray.Create("Successfully stopped the application.");
}


// Make the implicit Program class public so test projects can access it
public partial class Program { }

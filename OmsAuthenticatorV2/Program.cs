using System.Collections.Immutable;
using OmsAuthenticator;
using OmsAuthenticator.Api.V1;
using OmsAuthenticator.Api.V2;
using OmsAuthenticator.ApiAdapters;
using OmsAuthenticator.ApiAdapters.GISMT.V3;
using OmsAuthenticator.Configuration;
using OmsAuthenticator.Framework;
using OmsAuthenticator.Signing;

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
var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
var configuration = app.Services.GetRequiredService<IConfiguration>();

var cache = new TokenCache(systemTime);

AuthenticatorConfig.Create(configuration)
    .Convert(GetAdapterInstances)
    .Match(
        onSuccess: StartApplication,
        onFailure: PrintMessages);

void StartApplication(IEnumerable<IOmsTokenAdapter> adapters)
{
    foreach (var adapter in adapters)
    {
        var provider = new TokenProvider(cache, adapter);

        var controllerV1 = new TokenControllerV1(provider);
        // OMS token, semi-backward compatibility. Applications can use this API with configuration change.
        app.MapGet($"/api/v1/{adapter.PathSegment}/oms/token/", controllerV1.GetAsync);
        app.MapPost($"/api/v1/{adapter.PathSegment}/oms/token/", controllerV1.PostAsync);

        var controllerV2 = new TokenControllerV2(provider);
        // OMS token. New version of API. Applications need new client to use this API.
        app.MapGet($"/api/v2/{adapter.PathSegment}/oms/token/", controllerV2.GetOmsTokenAsync);
        // TRUE API token. New API.
        app.MapGet($"/api/v2/{adapter.PathSegment}/true/token/", controllerV2.GetTrueTokenAsync);
    }

    // Backward compatibility. Applications can use this API without any changes.
    var controller = new TokenControllerV1(new TokenProvider(cache, adapters.First()));
    app.MapGet($"/oms/token/", controller.GetAsync);
    app.MapPost($"/oms/token/", controller.PostAsync);
    // TODO: add DTABAC adapter and controller here

    app.Run();
}

void PrintMessages(ImmutableArray<string> errors)
{
    foreach (var message in errors)
    {
        Console.WriteLine(message);
    }
}

Result<IEnumerable<IOmsTokenAdapter>> GetAdapterInstances(AuthenticatorConfig config)
{
    var signData = new ConsoleSignData(config.Signer.Path);

    return Result.Combine(config.TokenProviders.Select(CreateAdapterInstance));

    Result<IOmsTokenAdapter> CreateAdapterInstance(TokenProviderConfig tokenProviderConfig) =>
        tokenProviderConfig.AdapterName switch
        {
            GisAdapterV3.AdapterName => Result.Success(GetGisAdapterV3(tokenProviderConfig)),
            _ => Result.Failure<IOmsTokenAdapter>($"Adapter '{tokenProviderConfig.AdapterName}' is not supported."),
        };

    IOmsTokenAdapter GetGisAdapterV3(TokenProviderConfig tokenProviderConfig) =>
        new GisAdapterV3(tokenProviderConfig, httpClientFactory, systemTime, signData);
}

// Make the implicit Program class public so test projects can access it
public partial class Program { }

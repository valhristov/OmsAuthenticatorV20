using System.Collections.Immutable;
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

void StartApplication(ImmutableArray<IOmsTokenAdapter> adapters)
{
    foreach (var adapter in adapters)
    {
        app.MapGet($"/api/v2/{adapter.PathSegment}/oms/token/", new TokenControllerV2(cache, adapter).GetOmsTokenAsync);
    }

    app.Run();
}

void PrintMessages(ImmutableArray<string> errors)
{
    foreach (var message in errors)
    {
        Console.WriteLine(message);
    }
}

Result<ImmutableArray<IOmsTokenAdapter>> GetAdapterInstances(AuthenticatorConfig config)
{
    var signData = new ConsoleSignData(config.Signer.Path);

    return Result.Success(config.TokenProviders.Select(CreateAdapterInstance).ToImmutableArray());

    IOmsTokenAdapter CreateAdapterInstance(TokenProviderConfig tokenProviderConfig) =>
        tokenProviderConfig.AdapterName switch
        {
            GisAdapterV3.AdapterName => (IOmsTokenAdapter)new GisAdapterV3(tokenProviderConfig, httpClientFactory, systemTime, signData),
            _ => throw new NotSupportedException($"Adapter '{tokenProviderConfig.AdapterName}' is not supported."),
        };
}

// Make the implicit Program class public so test projects can access it
public partial class Program { }

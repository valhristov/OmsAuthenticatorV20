using System.Collections.Immutable;
using OmsAuthenticator;
using OmsAuthenticator.Api.V1;
using OmsAuthenticator.Api.V2;
using OmsAuthenticator.ApiAdapters;
using OmsAuthenticator.ApiAdapters.GISMT.V3;
using OmsAuthenticator.Configuration;
using OmsAuthenticator.Framework;
using OmsAuthenticator.Signing;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.RollingFile("Logs\\{Date}.log", fileSizeLimitBytes:100_000_000)
    .CreateLogger();
Log.Information("=======================================================================");

var builder = WebApplication.CreateBuilder(
    new WebApplicationOptions
    {
        ContentRootPath = AppContext.BaseDirectory, // needed for builder.Host.UseWindowsService() 
        Args = args,
        ApplicationName = System.Diagnostics.Process.GetCurrentProcess().ProcessName,
    });

// Add services that should be replaceable in the unit tests
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ISystemTime>(new SystemTime());

builder.Host.UseSerilog();
builder.Host.UseWindowsService();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) { }

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();

// Instantiate singletons
var systemTime = app.Services.GetRequiredService<ISystemTime>();
var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
var configuration = app.Services.GetRequiredService<IConfiguration>();
var cache = new TokenCache(systemTime);

// Parse configuration
AuthenticatorConfig.Create(configuration)
// Instantiate GIS MT API adapters
    .Convert(GetAdapterInstances)
// Register API endpoints and start application
    .Match(
        onSuccess: StartApplication,
// When any of the operations above fails, print errors here
        onFailure: PrintMessages);

void StartApplication(IEnumerable<IOmsTokenAdapter> adapters)
{
    foreach (var adapter in adapters)
    {
        var provider = new TokenProvider(cache, adapter);

        var tokenControllerV1 = new TokenControllerV1(provider);
        // OMS token, semi-backward compatibility. Applications can use this API with configuration change.
        MapGet($"/api/v1/{adapter.PathSegment}/oms/token/", tokenControllerV1.GetAsync);
        MapPost($"/api/v1/{adapter.PathSegment}/oms/token/", tokenControllerV1.PostAsync);

        var tokenControllerV2 = new TokenControllerV2(provider);
        // OMS token. New version of API. Applications need new client to use this API.
        MapGet($"/api/v2/{adapter.PathSegment}/oms/token/", tokenControllerV2.GetOmsTokenAsync);
        // TRUE API token. New API.
        MapGet($"/api/v2/{adapter.PathSegment}/true/token/", tokenControllerV2.GetTrueTokenAsync);
        // Signature. New version of API. Applications need new client to use this API.
        var signatureControllerV2 = new SignatureControllerV2(adapter);
        MapPost($"/api/v2/{adapter.PathSegment}/sign/", signatureControllerV2.PostAsync);
    }

    // TODO: add signature controller here

    // Backward compatibility. Applications can use these APIs without any changes.
    var controller = new TokenControllerV1(new TokenProvider(cache, adapters.First()));
    MapGet($"/oms/token/", controller.GetAsync);
    MapPost($"/oms/token/", controller.PostAsync);
    var signatureControllerV1 = new SignatureControllerV1(adapters.First());
    MapPost($"/api/v1/signature/", signatureControllerV1.PostAsync);
    // TODO: add DTABAC adapter and controller here

    app.Run();

    void MapGet(string path, Delegate @delegate) =>
        InvokeWithMessage(() => app.MapGet(path, @delegate), $"Mapping URL 'GET {path}'");

    void MapPost(string path, Delegate @delegate) =>
        InvokeWithMessage(() => app.MapPost(path, @delegate), $"Mapping URL 'POST {path}'");

    void InvokeWithMessage(Action action, string message)
    {
        Log.Information(message);
        action?.Invoke();
    }
}

void PrintMessages(ImmutableArray<string> errors)
{
    foreach (var message in errors)
    {
        Log.Error(message);
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

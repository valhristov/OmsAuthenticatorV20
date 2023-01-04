using System.Collections.Immutable;
using OmsAuthenticator;
using OmsAuthenticator.Api.V1;
using OmsAuthenticator.Api.V2;
using OmsAuthenticator.ApiAdapters;
using OmsAuthenticator.ApiAdapters.DTABAC.V0;
using OmsAuthenticator.ApiAdapters.GISMT.V3;
using OmsAuthenticator.Configuration;
using OmsAuthenticator.Framework;
using OmsAuthenticator.Signing;
using Serilog;
using Serilog.Events;

Directory.SetCurrentDirectory(AppContext.BaseDirectory); // needed to locate SignData.exe

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.RollingFile(Path.GetFullPath("Logs\\{Date}.log"), fileSizeLimitBytes:100_000_000)
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

app.UseHttpsRedirection();

app.UseSerilogRequestLogging(options =>
{
    // Customize the message template
    options.MessageTemplate = "{RemoteIpAddress} {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.IncludeQueryInRequestPath = true;
    // Attach additional properties to the request completion event
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        ////diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value); // uncomment if needed
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
    };
}); 

// Instantiate singletons
var systemTime = app.Services.GetRequiredService<ISystemTime>();
var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
var configuration = app.Services.GetRequiredService<IConfiguration>();
var tokenStore = new TokenStore(new TokenCache(systemTime));

// Parse configuration
AuthenticatorConfig.Create(configuration)
    // Instantiate GIS MT API adapters
    .Convert(GetAdapterInstances)
    .Match(
        onSuccess: StartApplication, // Register API endpoints and start application
        onFailure: PrintMessages); // When any of the operations above fails, print errors here

void StartApplication(IEnumerable<ITokenAdapter> adapters)
{
    foreach (var adapter in adapters)
    {
        if (adapter is IOmsTokenAdapter omsTokenAdapter)
        {
            // OMS token, Old API. Applications can use this API with configuration change.
            var tokenControllerV1 = new OmsTokenControllerV1(tokenStore, omsTokenAdapter);
            MapGet($"/api/v1/{adapter.PathSegment}/oms/token/", tokenControllerV1.GetAsync);
            MapPost($"/api/v1/{adapter.PathSegment}/oms/token/", tokenControllerV1.PostAsync);

            // OMS token. New API. Applications need new client to use this API.
            var omsTokenControllerV2 = new OmsTokenControllerV2(tokenStore, omsTokenAdapter);
            MapGet($"/api/v2/{adapter.PathSegment}/oms/token/", omsTokenControllerV2.GetTokenAsync);
        }

        if (adapter is ITrueTokenAdapter trueTokenAdapter)
        {
            // TRUE API token. New API.
            var trueTokenControllerV2 = new TrueTokenControllerV2(tokenStore, trueTokenAdapter);
            MapGet($"/api/v2/{adapter.PathSegment}/true/token/", trueTokenControllerV2.GetTokenAsync);
        }

        if (adapter is ISignatureAdapter signatureAdapter)
        {
            // Signature. New version of API. Applications need new client to use this API.
            var signatureControllerV2 = new SignatureControllerV2(signatureAdapter);
            MapPost($"/api/v2/{adapter.PathSegment}/sign/", signatureControllerV2.PostAsync);
        }
    }

    // Backward compatibility. Applications can use these APIs without any changes.
    var controller = new OmsTokenControllerV1(tokenStore, adapters.OfType<IOmsTokenAdapter>().First());
    MapGet($"/oms/token/", controller.GetAsync);
    MapPost($"/oms/token/", controller.PostAsync);
    var signatureControllerV1 = new SignatureControllerV1(adapters.OfType<ISignatureAdapter>().First());
    MapPost($"/api/v1/signature/", signatureControllerV1.PostAsync);

    try
    {
        app.Run();
    }
    catch (Exception e)
    {
        Log.Error(e.ToString());
        throw;
    }

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

Result<IEnumerable<ITokenAdapter>> GetAdapterInstances(AuthenticatorConfig config)
{
    var signData = new ConsoleSignData(config.Signer.Path);

    return Result.Combine(config.TokenProviders.Select(CreateAdapterInstance));

    Result<ITokenAdapter> CreateAdapterInstance(TokenProviderConfig tokenProviderConfig) =>
        tokenProviderConfig.AdapterName switch
        {
            GisAdapterV3.AdapterName => Result.Success(GetGisAdapterV3(tokenProviderConfig)),
            DtabacAdapterV0.AdapterName => Result.Success(GetDtabacAdapterV3(tokenProviderConfig)),
            _ => Result.Failure<ITokenAdapter>($"Adapter '{tokenProviderConfig.AdapterName}' is not supported."),
        };

    ITokenAdapter GetGisAdapterV3(TokenProviderConfig tokenProviderConfig) =>
        new GisAdapterV3(tokenProviderConfig, httpClientFactory, systemTime, signData);

    ITokenAdapter GetDtabacAdapterV3(TokenProviderConfig tokenProviderConfig) =>
        new DtabacAdapterV0(tokenProviderConfig, httpClientFactory, systemTime);
}

// Make the implicit Program class public so test projects can access it
public partial class Program { }

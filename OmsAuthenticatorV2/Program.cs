using OmsAuthenticator;
using OmsAuthenticator.ApiAdapters.DTABAC.V0;
using OmsAuthenticator.ApiAdapters.GISMT.V3;
using OmsAuthenticator.Framework;

var builder = WebApplication.CreateBuilder(args);

// Add HttpClientFactory to the container in order to be able to mock it in the tests
builder.Services.AddHttpClient(OmsTokenAdapter.HttpClientName, client => client.BaseAddress = new Uri("http://markirovka")); // TODO: get from config
builder.Services.AddHttpClient(DtabacTokenAdapter.HttpClientName, client => client.BaseAddress = new Uri("http://dtabac")); // TODO: get from config
builder.Services.AddSingleton<ISystemTime>(new SystemTime());

builder.Host.UseWindowsService();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) { }

app.UseHttpsRedirection();

var expiration = TimeSpan.FromHours(8); // TODO: get from config

var systemTime = app.Services.GetRequiredService<ISystemTime>();

var cache = new AsyncResultCache<TokenKey, Token>(expiration, () => systemTime.UtcNow);

// Obtains OMS tokens from GIS MT API V3
var omsTokenAdapter = new OmsTokenAdapter(app.Services.GetRequiredService<IHttpClientFactory>(), token => new Token(token, systemTime.UtcNow.Add(expiration)));

var tokenControllerV1 = new OmsAuthenticator.Api.V1.TokenController(cache, omsTokenAdapter);
app.MapGet("/oms/token", tokenControllerV1.GetAsync);
app.MapPost("/oms/token", tokenControllerV1.PostAsync);

// Obtains OMS tokens from GIS MT API V3
var dtabacTokenAdapter = new DtabacTokenAdapter(app.Services.GetRequiredService<IHttpClientFactory>(), token => new Token(token, systemTime.UtcNow.Add(expiration)));

var dtabacControllerV1 = new OmsAuthenticator.Api.V1.TokenController(cache, omsTokenAdapter);
app.MapGet("/dtabac/token", tokenControllerV1.GetAsync);
app.MapPost("/dtabac/token", tokenControllerV1.PostAsync);

// TODO: omsTokenControllerV2

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program { }

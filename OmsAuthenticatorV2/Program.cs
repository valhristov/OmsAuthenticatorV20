using OmsAuthenticator;
using OmsAuthenticator.ApiAdapters.GISMT.V3;
using OmsAuthenticator.Framework;

var builder = WebApplication.CreateBuilder(args);

// Add HttpClientFactory to the container in order to be able to mock it in the tests
builder.Services.AddHttpClient(OmsTokenAdapter.HttpClientName, client => client.BaseAddress = new Uri("http://markirovka")); // TODO: get from config

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) { }

app.UseHttpsRedirection();

var expiration = TimeSpan.FromHours(8); // TODO: get from config

var cache = new AsyncResultCache<TokenKey, Token>(expiration);

// Obtains OMS tokens from GIS MT API V3
var omsTokenAdapter = new OmsTokenAdapter(app.Services.GetRequiredService<IHttpClientFactory>(), token => new Token(token, DateTimeOffset.UtcNow.Add(expiration)));

var tokenControllerV1 = new OmsAuthenticator.Api.V1.TokenController(cache, omsTokenAdapter);
app.MapGet("/oms/token", tokenControllerV1.GetAsync);
app.MapPost("/oms/token", tokenControllerV1.PostAsync);

// TODO: omsTokenControllerV2

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program { }
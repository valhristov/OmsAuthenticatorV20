using OmsAuthenticator.Api.V1;
using OmsAuthenticator.Framework;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var expiration = TimeSpan.FromHours(8); // TODO: get from config

var cache = new AsyncResultCache<TokenKey, TokenResponse>(expiration);

var tokenController = new TokenController();

app.MapGet("/oms/token", tokenController.GetAsync);
app.MapPost("/oms/token", tokenController.PostAsync);

app.Run();

public record TokenKey(string OmsId, string ConnectionId)
{
    public string? RequestId { get; set; }
}

// Make the implicit Program class public so test projects can access it
public partial class Program { }
using System.Net;
using MongoDB.Driver;
using MongoDB.Entities;
using Polly;
using Polly.Extensions.Http;
using SearchService.Data;
using SearchService.Models;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// AddHttpClient<AuctionSvcHttpClient>(): Registers a typed HttpClient for AuctionSvcHttpClient
// .AddPolicyHandler(GetPolicy()): Attaches the retry policy we defined to this HttpClient
//   - This means every HTTP call made by AuctionSvcHttpClient will automatically be wrapped in this policy.
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthorization();

app.MapControllers();

// Register lets you provide a callback to run once the application is fully running
app.Lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        await DbInitializer.InitDb(app);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
});

app.Run();

// defines a resilience policy for HTTP calls made through HttpClient
// A Polly policy defines rules for handling faults (errors, exceptions, etc.).
// This particular policy handles async methods that return HttpResponseMessage (i.e., HTTP requests).

static IAsyncPolicy<HttpResponseMessage> GetPolicy()
    => HttpPolicyExtensions
        .HandleTransientHttpError() // Handles transient (temporary) errors in HTTP requests
        .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound) // Adds extra retry condition: If the server returns 404 Not Found, we still want to retry
        .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3)); // Defines the retry strategy: Retry forever(no limit). Wait 3 seconds between each retry
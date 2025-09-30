using System.Net;
using MassTransit;
using MongoDB.Driver;
using MongoDB.Entities;
using Polly;
using Polly.Extensions.Http;
using SearchService.Consumers;
using SearchService.Data;
using SearchService.Models;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Gets all the assemblies currently loaded in the application domain (basically all the DLLs making up your app)
// AutoMapper will scan these assemblies to look for any mapping profiles you’ve defined
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
// AddHttpClient<AuctionSvcHttpClient>(): Registers a typed HttpClient for AuctionSvcHttpClient
// .AddPolicyHandler(GetPolicy()): Attaches the retry policy we defined to this HttpClient
//   - This means every HTTP call made by AuctionSvcHttpClient will automatically be wrapped in this policy.
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());
builder.Services.AddMassTransit(x =>
{
    // Scan the namespace where AuctionCreatedConsumer is defined (that is SearchService.Consumers), and automatically register all consumers in that namespace into the DI container.
    x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();

    x.AddConsumersFromNamespaceContaining<AuctionUpdatedConsumer>();

    x.AddConsumersFromNamespaceContaining<AuctionDeletedConsumer>();

    // Endpoint name formatter controls how MassTransit names the RabbitMQ queues/exchanges for each consumer
    // KebabCaseEndpointNameFormatter formats names in kebab-case (lowercase, words separated by -)
    // "search" → a prefix for all endpoints created by this service, like "search-auction-created"
    // false → whether to include the namespace in the queue name
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));

    // Use RabbitMQ as the transport layer
    // 'context' gives access to the current DI scope (so you can resolve configs, logging, etc.)
    // 'cfg' is the RabbitMQ configuration builder, where you specify how MassTransit should behave
    x.UsingRabbitMq((context, cfg) =>
    {
        // Create a receive endpoint (queue) named search-auction-created in RabbitMQ, and configure how messages arriving at that queue should be handled.
        cfg.ReceiveEndpoint("search-auction-created", e =>
        {
            // Adds a retry policy for message handling
            // MassTransit will automatically retry processing that same message up to 5 times.
            // If it still fails after retries, the message will be moved to the _error queue (e.g., search-auction-created_error) for later inspection.
            e.UseMessageRetry(r => r.Interval(5, 5)); // Retry 5 times, every 5 seconds

            // Registers the AuctionCreatedConsumer with this endpoint.
            // when a message of type AuctionCreated arrives in search-auction-created, this consumer will be invoked.
            e.ConfigureConsumer<AuctionCreatedConsumer>(context);
        });

        // a helper method that tells MassTransit:
        // Automatically create and configure receive endpoints for any consumers, sagas, 
        // or activities that you’ve registered in the DI container
        cfg.ConfigureEndpoints(context);
    });
});

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
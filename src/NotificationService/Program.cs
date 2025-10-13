using MassTransit;
using NotificationService.Consumers;
using NotificationService.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    // Find all consumers that are in the same namespace as the given type (AuctionCreatedConsumer) and automatically register them
    x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();

    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("nt", false));

    // Use RabbitMQ as the transport layer
    // 'context' gives access to the current DI scope (so you can resolve configs, logging, etc.)
    // 'cfg' is the RabbitMQ configuration builder, where you specify how MassTransit should behave
    x.UsingRabbitMq((context, cfg) =>
    {
        // configures the connection to the RabbitMQ broker
        cfg.Host(builder.Configuration["RabbitMq:Host"], "/", h =>
        {
            h.Username(builder.Configuration.GetValue("RabbitMq:Username", "guest"));
            h.Password(builder.Configuration.GetValue("RabbitMq:Password", "guest"));
        });

        // a helper method that tells MassTransit:
        // Automatically create and configure receive endpoints for any consumers, sagas, 
        // or activities that you’ve registered in the DI container
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddSignalR();

var app = builder.Build();

// Create a WebSocket (or fallback) endpoint at /notificationHub,
// and connect it to my NotificationHub class
app.MapHub<NotificationHub>("/notifications");

app.Run();

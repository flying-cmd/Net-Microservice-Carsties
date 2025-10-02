using AuctionService.Consumers;
using AuctionService.Data;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<AuctionContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddMassTransit(x =>
{
    // outbox to solve inconsistent data
    // outbox solves:
    //  Events are first saved in the same database transaction as your domain changes.
    //  A background process later reads the outbox table and publishes the events reliably.
    //  Guarantees atomicity between DB writes and message publishing.
    // AddEntityFrameworkOutbox<AuctionContext>: Configures MassTransit to use the Entity Framework Outbox with AuctionContext (EF Core DbContext)
    x.AddEntityFrameworkOutbox<AuctionContext>(o =>
    {
        // Controls how often the outbox processor polls the database for new messages. If there are new messages in the outbox, it publishes them. If not, it just waits another 10 seconds before checking again.
        // every 10 seconds, it checks for pending events to publish
        o.QueryDelay = TimeSpan.FromSeconds(10);

        // Configures the outbox to use PostgreSQL
        o.UsePostgres();

        // Enables the Bus Outbox
        // This prevents duplicate messages or messages published without DB commit.
        o.UseBusOutbox();
    });

    // scans the namespace where AuctionCreatedFaultConsumer lives and automatically registers all consumers in that namespace into the ASP.NET Core DI container
    x.AddConsumersFromNamespaceContaining<AuctionCreatedFaultConsumer>();

    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("auction", false));

    // Use RabbitMQ as the transport layer
    // 'context' gives access to the current DI scope (so you can resolve configs, logging, etc.)
    // 'cfg' is the RabbitMQ configuration builder, where you specify how MassTransit should behave
    x.UsingRabbitMq((context, cfg) =>
    {
        // a helper method that tells MassTransit:
        // Automatically create and configure receive endpoints for any consumers, sagas, 
        // or activities that you’ve registered in the DI container
        cfg.ConfigureEndpoints(context);
    });
});

// Registers authentication services
// JwtBearerDefaults.AuthenticationScheme = "Bearer"
// uses JWT bearer authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    // Adds JWT bearer handler with configuration options
    .AddJwtBearer(options =>
    {
        // Authority = the base URL of your IdentityServer
        // Used for:
        //     - Validating the token’s iss(issuer) claim.
        //     - Fetching public signing keys(via the OIDC discovery endpoint: /.well - known / openid - configuration).
        // By pointing here, your API knows where valid tokens must come from.
        options.Authority = builder.Configuration["IdentityServiceUrl"];
        // Setting this to false lets you use HTTP during development.
        options.RequireHttpsMetadata = false;
        // Disables audience validation
        // Normally, the token must have an 'aud' (audience) claim that matches your API's identifier.
        // Setting false means your API doesn’t care which 'aud' is in the token — it only checks the issuer and signature.
        options.TokenValidationParameters.ValidateAudience = false;
        // Tells ASP.NET Core which claim to treat as the user’s User.Identity.Name
        options.TokenValidationParameters.NameClaimType = "username";
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
// Purpose: Figures out “Who is the user?” for each incoming request.
// 1. Looks at the request (e.g., checks the Authorization: Bearer <token> header, cookies, etc.)
// 2. Validates the credentials/token.
// 3. If valid, creates a ClaimsPrincipal (the user identity) and attaches it to HttpContext.User.
app.UseAuthentication();

// Purpose: Decides “Is the user allowed to do this?”
// 1. Reads HttpContext.User (which was set by authentication).
// 2. Applies your authorization policies (like [Authorize], roles, claims requirements).
// 3. Blocks access if the user doesn’t meet the requirements
app.UseAuthorization();
// Authentication = who you are.
// Authorization = what you can do.

app.MapControllers();

try
{
    DbInitializer.InitDb(app);
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}

app.Run();

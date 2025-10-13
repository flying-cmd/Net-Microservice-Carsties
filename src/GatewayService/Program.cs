using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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

// Adds CORS support to the ASP.NET Core Dependency Injection container
builder.Services.AddCors(options =>
{
    options.AddPolicy("customPolicy", b =>
    {
        // Note: When you use AllowCredentials(), you cannot combine it with AllowAnyOrigin(). You must specify explicit origins with .WithOrigins()
        b.AllowAnyHeader() // Allows any HTTP headers in the request
            .AllowAnyMethod() // Allows any HTTP method in the request
            .AllowCredentials() // Allows cookies, authorization headers, or client certificates to be sent from the frontend
            .WithOrigins(builder.Configuration["ClientApp"]); // Restricts allowed origins (frontend URLs) to only the value specified in your app configuration
    });
});

var app = builder.Build();

app.UseCors();

app.MapReverseProxy();

app.UseAuthentication();
app.UseAuthorization();

app.Run();

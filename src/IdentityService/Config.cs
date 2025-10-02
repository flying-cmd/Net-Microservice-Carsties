using Duende.IdentityServer.Models;

namespace IdentityService;

public static class Config
{
    // Identity Resources represent information (claims) about a user that a client can request.
    // For example: "Who is the user?", "What is their profile?"
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
        };

    // API Scopes define what APIs a client can access.
    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("auctionApp", "Auction app full access"),
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            new Client
            {
                ClientId = "postman",
                ClientName = "Postman",
                // auctionApp → gives an access token for the Auction API.
                // profile → allows returning profile claims (like name, preferred_username).
                // openid → tells IdentityServer: "include an ID token with the user’s subject identifier".
                // Example ID token payload (simplified):
                /*
                {
                    "iss": "https://identity.example.com",
                    "sub": "12345",              // unique user ID (because of openid)
                    "name": "Alice Johnson",     // because of profile scope
                    "aud": "postman",
                    "iat": 1727890000,
                    "exp": 1727893600
                }
                */
                AllowedScopes = {"openid", "profile", "auctionApp"},
                // Where to redirect after authentication
                RedirectUris = {"https://www.getpostman.com/oauth2/callback"},
                ClientSecrets = new[] {new Secret("NotASecret".Sha256())},
                // Specifies the OAuth2 grant type this client can use
                // ResourceOwnerPassword = Resource Owner Password Credentials flow
                // The client directly sends username + password to IdentityServer
                AllowedGrantTypes = {GrantType.ResourceOwnerPassword},
            }
        };
}

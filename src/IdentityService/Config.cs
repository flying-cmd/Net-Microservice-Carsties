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

    public static IEnumerable<Client> Clients(IConfiguration config) =>
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
            },
            new Client
            {
                // unique ID for the client application, used by the client when requesting tokens
                ClientId = "nextApp",
                ClientName = "nextApp", // a human-readable name
                ClientSecrets = {new Secret("secret".Sha256())},
                // Grant types are the OAuth2 flows the client is allowed to use:
                //    - GrantTypes.Code → Authorization Code flow (secure, recommended for SPAs).
                //        - User logs in → gets an authorization code → exchanges it for tokens.
                //    - GrantTypes.ClientCredentials → Client Credentials flow (no user, just app identity).
                //        - Useful for background jobs or APIs talking to APIs.
                // So this client can do both:
                // Act as a logged-in user (code flow).
                // Or act as a system client (client credentials).
                AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                // PKCE (Proof Key for Code Exchange) is an extra security layer for code flow, mainly for public clients like SPAs or mobile apps.
                RequirePkce = false,
                // Defines where IdentityServer will send the user after login.
                RedirectUris = {config["ClientApp"] + "/api/auth/callback/id-server"},
                // This means the client can receive a refresh token.
                // Refresh tokens let the client silently renew access tokens without forcing the user to log in again.
                AllowOfflineAccess = true,
                AllowedScopes = {"openid", "profile", "auctionApp"},
                AccessTokenLifetime = 3600*24*30,
                AlwaysIncludeUserClaimsInIdToken = true
            }
        };
}

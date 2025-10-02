using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using IdentityService.Models;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Services;

// IProfileService, which IdentityServer uses to fetch user claims at token-issuance time.
// Whenever IdentityServer needs to build a token, it will call this service.
public class CustomerProfileService : IProfileService
{
    // Injected UserManager<ApplicationUser> — used to query users and their claims
    public readonly UserManager<ApplicationUser> _userManager;

    public CustomerProfileService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // Called when IdentityServer is about to issue a token (ID token or access token)
    // Use it to decide which claims should be included in the token
    // context contains:
    // Subject → the user(a ClaimsPrincipal).
    // RequestedClaimTypes → what claims were requested by the client.
    // IssuedClaims → the claims you add will be included in the token.
    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        // Gets the actual ApplicationUser from the Identity DB, based on the current subject (context.Subject = the user principal).
        var user = await _userManager.GetUserAsync(context.Subject);

        // Loads all claims already assigned to this user from the DB (AspNetUserClaims table).
        var existingClaims = await _userManager.GetClaimsAsync(user);

        // Creates a new claim list
        var claims = new List<Claim>
        {
            new Claim("username", user.UserName)
        };

        // Adds the custom claims you just built (username) to the issued claims list
        context.IssuedClaims.AddRange(claims);

        // Ensures that the user's full name is also included in the token.
        context.IssuedClaims.Add(existingClaims.FirstOrDefault(x => x.Type == JwtClaimTypes.Name));
    }

    // Called by IdentityServer to check if the user account is active
    public Task IsActiveAsync(IsActiveContext context)
    {
        return Task.CompletedTask;
    }
}

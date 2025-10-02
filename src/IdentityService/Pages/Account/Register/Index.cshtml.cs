using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Duende.IdentityModel;
using IdentityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace IdentityService.Pages.Account.Register
{
    // SecurityHeaders → custom attribute (likely middleware or filter) to add security headers (e.g., X-Frame-Options, CSP).
    // [AllowAnonymous] → allows access without authentication (since this is a registration page)
    [SecurityHeaders]
    [AllowAnonymous]
    public class Index : PageModel
    {
        // UserManager<T> is the ASP.NET Identity service for creating and managing users.
        // _userManager is injected through DI to handle user creation, claims, etc.
        private readonly UserManager<ApplicationUser> _userManager;

        public Index(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // BindProperty allows model binding from form input fields directly into this property.
        // This is where the registration form data goes when user submits
        [BindProperty]
        public RegisterViewModel Input { get; set; }

        [BindProperty]
        public bool RegiterSuccess { get; set; }

        // This is called when the page loads via HTTP GET (before form submission)
        // Initializes the Input view model and sets ReturnUrl (where the user should be redirected after registration).
        public void OnGet(string returnUrl)
        {
            Input = new RegisterViewModel()
            {
                ReturnUrl = returnUrl,
            };
        }

        // This runs when the form is submitted (HTTP POST)
        // Handles registration logic
        // Returns an IActionResult → can return Page(), Redirect(), etc.
        public async Task<IActionResult> OnPost()
        {
            if (Input.Button != "register")
            {
                // the ~ is a special character that represents the application root (the base URL of your site)
                return RedirectToPage("~");
            }

            // Ensures validation rules (e.g., required fields, password length) passed.
            if (ModelState.IsValid)
            {
                // Creates a new ApplicationUser (your Identity user entity)
                var user = new ApplicationUser
                {
                    UserName = Input.Username,
                    Email = Input.Email,
                    EmailConfirmed = true
                };

                // save the user into the Identity database
                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    // Adds a claim to the user (here: a Name claim containing the full name)
                    await _userManager.AddClaimsAsync(user, new Claim[]
                    {
                        // JwtClaimTypes.Name → standardized claim type used in IdentityServer / JWT.
                        new Claim(JwtClaimTypes.Name, Input.FullName)
                    });

                    RegiterSuccess = true;
                }
            }
            return Page();
        }
    }
}
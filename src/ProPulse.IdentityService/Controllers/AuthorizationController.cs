using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using ProPulse.IdentityService.Models;

namespace ProPulse.IdentityService.Controllers;

public class AuthorizationController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ILogger<AuthorizationController> logger) : Controller
{
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Try to retrieve the user principal stored in the authentication cookie and redirect to login page if not found
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (result is not { Succeeded: true })
        {
            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                        Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                });
        }

        var user = await userManager.GetUserAsync(result.Principal) ??
            throw new InvalidOperationException("The user details cannot be retrieved.");

        // Create a new claims principal
        var claims = new List<Claim>
        {
            new Claim(OpenIddictConstants.Claims.Subject, await userManager.GetUserIdAsync(user)),
            new Claim(OpenIddictConstants.Claims.Email, await userManager.GetEmailAsync(user) ?? string.Empty),
            new Claim(OpenIddictConstants.Claims.Name, user.DisplayName),
            new Claim("display_name", user.DisplayName).SetDestinations(OpenIddictConstants.Destinations.IdentityToken, OpenIddictConstants.Destinations.AccessToken)
        };

        // Add user roles to the claims
        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(OpenIddictConstants.Claims.Role, role));
        }

        var claimsIdentity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Set destinations for each claim
        foreach (var claim in claimsPrincipal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, request));
        }

        return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Token()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            // Retrieve the claims principal stored in the authorization code/refresh token
            var claimsPrincipal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

            return SignIn(claimsPrincipal!, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsClientCredentialsGrantType())
        {
            // Note: the client credentials are automatically validated by OpenIddict
            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            // Add a "scope" claim based on the scopes from the request
            identity.AddClaim(OpenIddictConstants.Claims.Subject, request.ClientId ?? "unknown_client");
            identity.AddClaim(OpenIddictConstants.Claims.Scope, request.Scope ?? "api");

            var claimsPrincipal = new ClaimsPrincipal(identity);
            claimsPrincipal.SetScopes(request.GetScopes());

            return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("The specified grant type is not supported.");
    }

    [HttpGet("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        // Ask ASP.NET Core Identity to delete the local cookie
        await signInManager.SignOutAsync();

        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties
            {
                RedirectUri = "/"
            });
    }

    private static IEnumerable<string> GetDestinations(Claim claim, OpenIddictRequest request)
    {
        // Note: by default, claims are NOT automatically included in the access and identity tokens.
        // To allow OpenIddict to serialize them, you must attach them a destination.
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Name:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (request.HasScope(OpenIddictConstants.Scopes.Profile))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case OpenIddictConstants.Claims.Email:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (request.HasScope(OpenIddictConstants.Scopes.Email))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case OpenIddictConstants.Claims.Role:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (request.HasScope("roles"))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            // Always include the claim in both the access and the identity tokens when the corresponding
            // scope was granted to ensure the client is authorized to use this custom claim.
            case "display_name" when request.HasScope(OpenIddictConstants.Scopes.Profile):
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }
}
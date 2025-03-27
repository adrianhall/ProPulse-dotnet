using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using ProPulse.IdentityService.Models;
using System.Security.Claims;

namespace ProPulse.IdentityService.Controllers;

[ApiController]
[Route("[controller]")]
public class UserinfoController(UserManager<ApplicationUser> userManager, ILogger<UserinfoController> logger) : ControllerBase
{
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo"), HttpPost("~/connect/userinfo")]
    public async Task<IActionResult> Userinfo()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            logger.LogInformation("New user object for user - issuing challenge");
            // Create a dictionary for authentication properties with correct nullability
            var properties = new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidToken,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified access token is bound to an account that no longer exists."
            };

            return Challenge(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(properties));
        }

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            // Note: the "sub" claim is a mandatory claim and must be included in the JSON response.
            [OpenIddictConstants.Claims.Subject] = await userManager.GetUserIdAsync(user),
            [OpenIddictConstants.Claims.Name] = user.DisplayName,
            [OpenIddictConstants.Claims.Email] = await userManager.GetEmailAsync(user) ?? string.Empty,
            [OpenIddictConstants.Claims.EmailVerified] = user.EmailConfirmed ? "true" : "false"
        };

        // Add the user's roles as claims
        var roles = await userManager.GetRolesAsync(user);
        if (roles.Any())
        {
            claims[OpenIddictConstants.Claims.Role] = roles.ToArray();
        }

        return Ok(claims);
    }
}
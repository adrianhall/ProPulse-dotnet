using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Propulse.IdentityService.Tests.Infrastructure;
using ProPulse.IdentityService.Models;

namespace Propulse.IdentityService.Tests.IntegrationTests;

public abstract class OAuthIntegrationTestBase : IClassFixture<IdentityServiceWebApplicationFactory>
{
    protected readonly IdentityServiceWebApplicationFactory _factory;
    protected readonly HttpClient _client;

    protected OAuthIntegrationTestBase(IdentityServiceWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    protected async Task<ApplicationUser> CreateUserAsync(string username, string email, string password, string displayName, params string[] roles)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure roles exist
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Create user
        var user = new ApplicationUser
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Assign roles
        if (roles.Any())
        {
            result = await userManager.AddToRolesAsync(user, roles);
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to assign roles: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        return user;
    }

    protected async Task<ApplicationUser> CreateClientApplicationAsync(string clientId, string clientSecret, string displayName, string[] redirectUris, string[] permissions)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var openIddictApplicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        // Create a client application user
        var clientUser = new ApplicationUser
        {
            UserName = clientId,
            Email = $"{clientId}@example.com",
            EmailConfirmed = true,
            DisplayName = displayName
        };

        var result = await userManager.CreateAsync(clientUser, clientSecret);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create client user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Register the corresponding OpenIddict application
        await openIddictApplicationManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = displayName,
            RedirectUris = { new Uri("https://localhost/callback") },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                OpenIddictConstants.Permissions.Prefixes.Scope + "api"
            }
        });

        return clientUser;
    }

    protected async Task<string> GetAuthorizationCodeAsync(string clientId, string redirectUri, string scope, ApplicationUser user)
    {
        // Ensure the client is authenticated
        var loginResponse = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = user.Email!,
            ["Password"] = "Password123!",
            ["RememberMe"] = "false"
        }));

        // Request authorization code
        var authorizeRequest = new HttpRequestMessage(HttpMethod.Get, 
            $"/connect/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}" + 
            $"&response_type=code&scope={Uri.EscapeDataString(scope)}&state=state&prompt=none");
        
        var authorizeResponse = await _client.SendAsync(authorizeRequest);
        authorizeResponse.EnsureSuccessStatusCode();

        // Extract the authorization code from the redirect URL
        var location = authorizeResponse.Headers.Location;
        if (location == null)
        {
            throw new Exception("No redirect location found in authorization response");
        }

        var query = System.Web.HttpUtility.ParseQueryString(location.Query);
        var code = query["code"];
        if (string.IsNullOrEmpty(code))
        {
            throw new Exception("No authorization code found in redirect URI");
        }

        return code;
    }

    protected async Task<JsonDocument> ExchangeCodeForTokenAsync(string clientId, string clientSecret, string code, string redirectUri)
    {
        // Exchange the authorization code for access token
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            })
        };

        var tokenResponse = await _client.SendAsync(tokenRequest);
        tokenResponse.EnsureSuccessStatusCode();

        var json = await tokenResponse.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    protected async Task<JsonDocument> GetUserInfoAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    protected async Task<JsonDocument> GetClientCredentialsTokenAsync(string clientId, string clientSecret, string scope)
    {
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["scope"] = scope
            })
        };

        var tokenResponse = await _client.SendAsync(tokenRequest);
        tokenResponse.EnsureSuccessStatusCode();

        var json = await tokenResponse.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    protected JsonElement? ExtractClaimsFromToken(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            throw new ArgumentException("Invalid JWT token format");
        }

        var payload = parts[1];
        // Add padding if needed
        while (payload.Length % 4 != 0)
        {
            payload += "=";
        }

        var jsonBytes = Convert.FromBase64String(payload);
        var jsonString = Encoding.UTF8.GetString(jsonBytes);
        return JsonDocument.Parse(jsonString).RootElement;
    }
}
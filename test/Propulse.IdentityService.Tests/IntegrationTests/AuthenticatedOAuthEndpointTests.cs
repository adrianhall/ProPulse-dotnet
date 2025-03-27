using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Propulse.IdentityService.Tests.Infrastructure;
using ProPulse.IdentityService.Models;
using Xunit.Abstractions;

namespace Propulse.IdentityService.Tests.IntegrationTests;

public class AuthenticatedOAuthEndpointTests : OAuthIntegrationTestBase
{
    private readonly ITestOutputHelper _output;
    private const string ClientId = "test-client";
    private const string ClientSecret = "Password123!";
    private const string RedirectUri = "https://localhost/callback";
    private const string Scope = "openid profile email roles api";

    public AuthenticatedOAuthEndpointTests(IdentityServiceWebApplicationFactory factory, ITestOutputHelper output) 
        : base(factory)
    {
        _output = output;
    }

    [Fact]
    public async Task CompleteAuthorizationFlow_ForAdministratorUser_HasAdministratorRoleClaim()
    {
        // Arrange
        await CreateClientApplicationAsync(
            ClientId, 
            ClientSecret, 
            "Test Client", 
            new[] { RedirectUri }, 
            new[] { Scope });

        var adminUser = await CreateUserAsync(
            "admin", 
            "admin@example.com", 
            "Password123!", 
            "Admin User", 
            "Administrator");

        // Act
        // 1. Get authorization code
        var authCode = await GetAuthorizationCodeAsync(ClientId, RedirectUri, Scope, adminUser);
        
        // 2. Exchange code for token
        var tokenResponse = await ExchangeCodeForTokenAsync(ClientId, ClientSecret, authCode, RedirectUri);
        
        // 3. Extract token for validation
        var accessToken = tokenResponse.RootElement.GetProperty("access_token").GetString();
        Assert.NotNull(accessToken);
        
        // 4. Check userinfo endpoint with token
        var userInfo = await GetUserInfoAsync(accessToken);

        // Assert
        // Validate token claims
        var tokenClaims = ExtractClaimsFromToken(accessToken);
        Assert.NotNull(tokenClaims);
        
        // Verify role claim exists and has Administrator value
        Assert.True(tokenClaims.Value.TryGetProperty("role", out var roleClaim));
        var role = roleClaim.GetString();
        Assert.Equal("Administrator", role);
        
        // Verify subject and name claims match
        Assert.True(tokenClaims.Value.TryGetProperty("sub", out var subClaim));
        Assert.Equal(adminUser.Id, subClaim.GetString());
        
        // Verify name claim matches
        Assert.True(tokenClaims.Value.TryGetProperty("name", out var nameClaim));
        Assert.Equal(adminUser.DisplayName, nameClaim.GetString());
        
        // Verify userinfo endpoint contains the same information
        Assert.Equal(adminUser.Id, userInfo.RootElement.GetProperty("sub").GetString());
        Assert.Equal("Administrator", userInfo.RootElement.GetProperty("role").GetString());
    }

    [Fact]
    public async Task CompleteAuthorizationFlow_ForAuthorUser_HasAuthorRoleClaim()
    {
        // Arrange
        await CreateClientApplicationAsync(
            ClientId, 
            ClientSecret, 
            "Test Client", 
            new[] { RedirectUri }, 
            new[] { Scope });

        var authorUser = await CreateUserAsync(
            "author", 
            "author@example.com", 
            "Password123!", 
            "Author User", 
            "Author");

        // Act
        // 1. Get authorization code
        var authCode = await GetAuthorizationCodeAsync(ClientId, RedirectUri, Scope, authorUser);
        
        // 2. Exchange code for token
        var tokenResponse = await ExchangeCodeForTokenAsync(ClientId, ClientSecret, authCode, RedirectUri);
        
        // 3. Extract token for validation
        var accessToken = tokenResponse.RootElement.GetProperty("access_token").GetString();
        Assert.NotNull(accessToken);
        
        // 4. Check userinfo endpoint with token
        var userInfo = await GetUserInfoAsync(accessToken);

        // Assert
        // Validate token claims
        var tokenClaims = ExtractClaimsFromToken(accessToken);
        Assert.NotNull(tokenClaims);
        
        // Verify role claim exists and has Author value
        Assert.True(tokenClaims.Value.TryGetProperty("role", out var roleClaim));
        var role = roleClaim.GetString();
        Assert.Equal("Author", role);
        
        // Verify userinfo endpoint contains the same information
        Assert.Equal(authorUser.Id, userInfo.RootElement.GetProperty("sub").GetString());
        Assert.Equal("Author", userInfo.RootElement.GetProperty("role").GetString());
    }

    [Fact]
    public async Task CompleteAuthorizationFlow_ForRegularUser_HasUserRoleClaim()
    {
        // Arrange
        await CreateClientApplicationAsync(
            ClientId, 
            ClientSecret, 
            "Test Client", 
            new[] { RedirectUri }, 
            new[] { Scope });

        var regularUser = await CreateUserAsync(
            "user", 
            "user@example.com", 
            "Password123!", 
            "Regular User", 
            "User");

        // Act
        // 1. Get authorization code
        var authCode = await GetAuthorizationCodeAsync(ClientId, RedirectUri, Scope, regularUser);
        
        // 2. Exchange code for token
        var tokenResponse = await ExchangeCodeForTokenAsync(ClientId, ClientSecret, authCode, RedirectUri);
        
        // 3. Extract token for validation
        var accessToken = tokenResponse.RootElement.GetProperty("access_token").GetString();
        Assert.NotNull(accessToken);
        
        // 4. Check userinfo endpoint with token
        var userInfo = await GetUserInfoAsync(accessToken);

        // Assert
        // Validate token claims
        var tokenClaims = ExtractClaimsFromToken(accessToken);
        Assert.NotNull(tokenClaims);
        
        // Verify role claim exists and has User value
        Assert.True(tokenClaims.Value.TryGetProperty("role", out var roleClaim));
        var role = roleClaim.GetString();
        Assert.Equal("User", role);
        
        // Verify userinfo endpoint contains the same information
        Assert.Equal(regularUser.Id, userInfo.RootElement.GetProperty("sub").GetString());
        Assert.Equal("User", userInfo.RootElement.GetProperty("role").GetString());
    }

    [Fact]
    public async Task CompleteAuthorizationFlow_ForUserWithMultipleRoles_HasAllRoleClaims()
    {
        // Arrange
        await CreateClientApplicationAsync(
            ClientId, 
            ClientSecret, 
            "Test Client", 
            new[] { RedirectUri }, 
            new[] { Scope });

        var multiRoleUser = await CreateUserAsync(
            "multirole", 
            "multirole@example.com", 
            "Password123!", 
            "Multi-Role User", 
            new[] { "Administrator", "Author", "User" });

        // Act
        // 1. Get authorization code
        var authCode = await GetAuthorizationCodeAsync(ClientId, RedirectUri, Scope, multiRoleUser);
        
        // 2. Exchange code for token
        var tokenResponse = await ExchangeCodeForTokenAsync(ClientId, ClientSecret, authCode, RedirectUri);
        
        // 3. Extract token for validation
        var accessToken = tokenResponse.RootElement.GetProperty("access_token").GetString();
        Assert.NotNull(accessToken);
        
        // 4. Check userinfo endpoint with token
        var userInfo = await GetUserInfoAsync(accessToken);

        // Assert
        // Validate token claims
        var tokenClaims = ExtractClaimsFromToken(accessToken);
        Assert.NotNull(tokenClaims);
        
        // Verify role claims exist and have all assigned values
        Assert.True(tokenClaims.Value.TryGetProperty("role", out var roleClaimElement));
        
        // If only one role, it will be a string; if multiple, it will be an array
        if (roleClaimElement.ValueKind == JsonValueKind.String)
        {
            var roleName = roleClaimElement.GetString();
            Assert.Contains(new[] { "Administrator", "Author", "User" }, r => r == roleName);
        }
        else if (roleClaimElement.ValueKind == JsonValueKind.Array)
        {
            var roles = new List<string>();
            foreach (var element in roleClaimElement.EnumerateArray())
            {
                roles.Add(element.GetString()!);
            }
            
            Assert.Contains("Administrator", roles);
            Assert.Contains("Author", roles);
            Assert.Contains("User", roles);
        }
        else
        {
            Assert.Fail($"Unexpected role claim format: {roleClaimElement.ValueKind}");
        }
        
        // Verify userinfo endpoint contains the same role information
        Assert.Equal(multiRoleUser.Id, userInfo.RootElement.GetProperty("sub").GetString());
        
        // Check roles in userinfo (could be array or single value)
        Assert.True(userInfo.RootElement.TryGetProperty("role", out var userInfoRoleClaim));
    }
}
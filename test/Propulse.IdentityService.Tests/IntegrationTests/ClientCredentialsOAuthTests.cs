using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Propulse.IdentityService.Tests.Infrastructure;
using ProPulse.IdentityService.Models;
using Xunit.Abstractions;

namespace Propulse.IdentityService.Tests.IntegrationTests;

public class ClientCredentialsOAuthTests : OAuthIntegrationTestBase
{
    private readonly ITestOutputHelper _output;
    private const string ClientId = "service-client";
    private const string ClientSecret = "Password123!";
    private const string Scope = "api roles";

    public ClientCredentialsOAuthTests(IdentityServiceWebApplicationFactory factory, ITestOutputHelper output) 
        : base(factory)
    {
        _output = output;
    }

    [Fact]
    public async Task ClientCredentials_WithValidClient_ReturnsValidToken()
    {
        // Arrange
        await CreateClientApplicationAsync(
            ClientId, 
            ClientSecret, 
            "Service Client", 
            new[] { "https://localhost/callback" }, 
            new[] { Scope });

        // Act
        var tokenResponse = await GetClientCredentialsTokenAsync(ClientId, ClientSecret, Scope);

        // Assert
        Assert.NotNull(tokenResponse);
        Assert.True(tokenResponse.RootElement.TryGetProperty("access_token", out _));
        Assert.True(tokenResponse.RootElement.TryGetProperty("token_type", out var tokenType));
        Assert.Equal("Bearer", tokenType.GetString());
        Assert.True(tokenResponse.RootElement.TryGetProperty("expires_in", out _));
    }

    [Fact]
    public async Task ClientCredentials_WithInvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        await CreateClientApplicationAsync(
            ClientId, 
            ClientSecret, 
            "Service Client", 
            new[] { "https://localhost/callback" }, 
            new[] { Scope });

        // Act
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = ClientId,
                ["client_secret"] = "wrong-secret",
                ["scope"] = Scope
            })
        };

        var response = await _client.SendAsync(tokenRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var error = JsonDocument.Parse(content).RootElement;
        Assert.Equal("invalid_client", error.GetProperty("error").GetString());
    }

    [Fact]
    public async Task ClientCredentials_WithValidClient_ProperScopesInToken()
    {
        // Arrange
        await CreateClientApplicationAsync(
            ClientId, 
            ClientSecret, 
            "Service Client", 
            new[] { "https://localhost/callback" }, 
            new[] { Scope });

        // Act
        var tokenResponse = await GetClientCredentialsTokenAsync(ClientId, ClientSecret, Scope);
        var accessToken = tokenResponse.RootElement.GetProperty("access_token").GetString();
        Assert.NotNull(accessToken);

        // Extract and validate token claims
        var tokenClaims = ExtractClaimsFromToken(accessToken);
        Assert.NotNull(tokenClaims);

        // Verify scope claim contains requested scopes
        Assert.True(tokenClaims.Value.TryGetProperty("scope", out var scopeClaim));
        var tokenScope = scopeClaim.GetString();
        Assert.Contains("api", tokenScope);
        Assert.Contains("roles", tokenScope);

        // Verify client_id is present as subject
        Assert.True(tokenClaims.Value.TryGetProperty("sub", out var subClaim));
        Assert.Equal(ClientId, subClaim.GetString());
    }
}
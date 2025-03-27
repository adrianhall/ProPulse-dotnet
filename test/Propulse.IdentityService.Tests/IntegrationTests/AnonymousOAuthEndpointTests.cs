using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Propulse.IdentityService.Tests.Infrastructure;
using Xunit.Abstractions;

namespace Propulse.IdentityService.Tests.IntegrationTests;

public class AnonymousOAuthEndpointTests : OAuthIntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    public AnonymousOAuthEndpointTests(IdentityServiceWebApplicationFactory factory, ITestOutputHelper output) 
        : base(factory)
    {
        _output = output;
    }

    [Fact]
    public async Task Authorize_WhenNotAuthenticated_RedirectsToLogin()
    {
        // Arrange
        var clientId = "test-client";
        var redirectUri = "https://localhost/callback";
        var scope = "openid profile email roles";

        await CreateClientApplicationAsync(
            clientId, 
            "test-secret", 
            "Test Client", 
            new[] { redirectUri }, 
            new[] { scope });

        // Act
        var response = await _client.GetAsync(
            $"/connect/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code&scope={Uri.EscapeDataString(scope)}&state=test-state");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location;
        Assert.NotNull(location);
        Assert.Contains("/Account/Login", location.PathAndQuery);
    }

    [Fact]
    public async Task Token_WithInvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "invalid-client",
                ["client_secret"] = "invalid-secret",
                ["scope"] = "api"
            })
        };

        // Act
        var response = await _client.SendAsync(tokenRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var error = JsonDocument.Parse(content).RootElement;
        Assert.Equal("invalid_client", error.GetProperty("error").GetString());
    }

    [Fact]
    public async Task UserInfo_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/connect/userinfo");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
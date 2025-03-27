using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ProPulse.IdentityService.Models;
using Propulse.IdentityService.Tests.Infrastructure;

namespace Propulse.IdentityService.Tests.IntegrationTests;

public class LogoutIntegrationTests(IdentityServiceWebApplicationFactory factory) 
    : IdentityServiceIntegrationTestBase(factory)
{
    [Fact]
    public async Task Logout_AuthenticatedUser_ShouldLogoutSuccessfully()
    {
        // Arrange - Create a user and login
        var email = $"logout-{Guid.NewGuid()}@example.com";
        var password = "Test1234!";
        var displayName = "Logout Test User";
        
        // Create confirmed user
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = displayName,
                EmailConfirmed = true
            };
            
            await userManager.CreateAsync(user, password);
        }
        
        // Login
        await LoginUserAsync(email, password);
        
        // Verify user is logged in
        var isLoggedInBefore = await IsUserLoggedInAsync();
        Assert.True(isLoggedInBefore);
        
        // Act - Logout
        var response = await Client.GetAsync("/Account/Logout");
        
        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        
        // Verify user is logged out
        var isLoggedInAfter = await IsUserLoggedInAsync();
        Assert.False(isLoggedInAfter);
    }

    [Fact]
    public async Task Logout_WithReturnUrl_ShouldRedirectToReturnUrl()
    {
        // Arrange - Create a user and login
        var email = $"logout-redirect-{Guid.NewGuid()}@example.com";
        var password = "Test1234!";
        var displayName = "Logout Redirect Test User";
        
        // Create confirmed user
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = displayName,
                EmailConfirmed = true
            };
            
            await userManager.CreateAsync(user, password);
        }
        
        // Login
        await LoginUserAsync(email, password);
        
        // Act - Logout with return URL
        var returnUrl = "/Account/Login";
        var response = await Client.GetAsync($"/Account/Logout?returnUrl={Uri.EscapeDataString(returnUrl)}");
        
        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var redirectUrl = ExtractLocationHeader(response);
        Assert.Equal(returnUrl, redirectUrl);
        
        // Verify user is logged out
        var isLoggedInAfter = await IsUserLoggedInAsync();
        Assert.False(isLoggedInAfter);
    }

    [Fact]
    public async Task Logout_WhenNotAuthenticated_ShouldRedirectToHomePage()
    {
        // Arrange - Ensure user is not logged in
        // Act - Try to logout
        var response = await Client.GetAsync("/Account/Logout");
        
        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var redirectUrl = ExtractLocationHeader(response);
        Assert.Equal("/", redirectUrl);
    }
}
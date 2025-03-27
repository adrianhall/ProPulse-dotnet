using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ProPulse.IdentityService.Models;
using Propulse.IdentityService.Tests.Infrastructure;

namespace Propulse.IdentityService.Tests.IntegrationTests;

public class LoginIntegrationTests(IdentityServiceWebApplicationFactory factory) 
    : IdentityServiceIntegrationTestBase(factory)
{
    [Fact]
    public async Task Login_WithValidCredentials_ShouldLoginUser()
    {
        // Arrange - Create a confirmed user
        var email = $"login-valid-{Guid.NewGuid()}@example.com";
        var password = "Test1234!";
        var displayName = "Login Test User";
        
        await CreateConfirmedUserAsync(email, password, displayName);
        
        // Act - Login with valid credentials
        var formData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["RememberMe"] = "false",
            ["ReturnUrl"] = BasicReturnUrl
        };

        var response = await PostFormWithAntiforgeryTokenAsync("/Account/Login", formData);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var redirectUrl = ExtractLocationHeader(response);
        Assert.Equal("/", redirectUrl);
        
        // Verify user is logged in
        var isLoggedIn = await IsUserLoggedInAsync();
        Assert.True(isLoggedIn);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnError()
    {
        // Arrange - Create a confirmed user
        var email = $"login-invalid-pass-{Guid.NewGuid()}@example.com";
        var password = "Test1234!";
        var displayName = "Login Test User";
        
        await CreateConfirmedUserAsync(email, password, displayName);
        
        // Act - Login with wrong password
        var formData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "WrongPassword1!",
            ["RememberMe"] = "false",
            ["ReturnUrl"] = BasicReturnUrl
        };

        var response = await PostFormWithAntiforgeryTokenAsync("/Account/Login", formData);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid login attempt", content);
        
        // Verify user is not logged in
        var isLoggedIn = await IsUserLoggedInAsync();
        Assert.False(isLoggedIn);
    }

    [Fact]
    public async Task Login_WithNonexistentEmail_ShouldReturnError()
    {
        // Arrange
        var nonExistentEmail = $"nonexistent-{Guid.NewGuid()}@example.com";
        
        // Act - Login with non-existent email
        var formData = new Dictionary<string, string>
        {
            ["Email"] = nonExistentEmail,
            ["Password"] = "Test1234!",
            ["RememberMe"] = "false",
            ["ReturnUrl"] = BasicReturnUrl
        };

        var response = await PostFormWithAntiforgeryTokenAsync("/Account/Login", formData);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid login attempt", content);
        
        // Verify user is not logged in
        var isLoggedIn = await IsUserLoggedInAsync();
        Assert.False(isLoggedIn);
    }

    [Fact]
    public async Task Login_WithUnconfirmedEmail_ShouldReturnError()
    {
        // Arrange - Create an unconfirmed user
        var email = $"login-unconfirmed-{Guid.NewGuid()}@example.com";
        var password = "Test1234!";
        var displayName = "Unconfirmed Test User";
        
        // Directly create a user without confirming email
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = displayName,
                EmailConfirmed = false
            };
            
            await userManager.CreateAsync(user, password);
        }
        
        // Act - Try to login
        var formData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["RememberMe"] = "false",
            ["ReturnUrl"] = BasicReturnUrl
        };

        var response = await PostFormWithAntiforgeryTokenAsync("/Account/Login", formData);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // The exact error message depends on configuration, but it should mention invalid login
        Assert.Contains("Invalid login attempt", content);
        
        // Verify user is not logged in
        var isLoggedIn = await IsUserLoggedInAsync();
        Assert.False(isLoggedIn);
    }

    [Fact]
    public async Task Login_WithRememberMe_ShouldSetPersistentCookie()
    {
        // Arrange - Create a confirmed user
        var email = $"login-remember-{Guid.NewGuid()}@example.com";
        var password = "Test1234!";
        var displayName = "Remember Me Test User";
        
        await CreateConfirmedUserAsync(email, password, displayName);
        
        // Act - Login with RememberMe set to true
        var formData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["RememberMe"] = "true",
            ["ReturnUrl"] = BasicReturnUrl
        };

        var response = await PostFormWithAntiforgeryTokenAsync("/Account/Login", formData);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        
        // Check if the Set-Cookie header contains persistent cookie settings
        // This is an approximation since we can't directly check the IsPersistent flag
        // in the actual authentication cookie due to test limitations
        var setCookieHeader = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
        Assert.NotNull(setCookieHeader);
        
        // The persistent cookie should have an expiry date (non-session cookie)
        // This is a simplistic check - in a real scenario we would check the actual expiry
        Assert.Contains("expires=", setCookieHeader.ToLowerInvariant());
    }

    [Fact]
    public async Task Login_WithLockedOutUser_ShouldRedirectToLockedOutPage()
    {
        // Arrange - Create a user that will be locked out
        var email = $"login-lockout-{Guid.NewGuid()}@example.com";
        var password = "Test1234!";
        var displayName = "Lockout Test User";
        
        await CreateConfirmedUserAsync(email, password, displayName);
        
        // Lock out the user
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            
            // Set a lockout end date in the future
            await userManager.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddDays(1));
        }
        
        // Act - Try to login with locked out user
        var formData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["RememberMe"] = "false",
            ["ReturnUrl"] = BasicReturnUrl
        };

        var response = await PostFormWithAntiforgeryTokenAsync("/Account/Login", formData);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var redirectUrl = ExtractLocationHeader(response);
        Assert.Contains("/Account/LockedOut", redirectUrl);
        
        // Verify user is not logged in
        var isLoggedIn = await IsUserLoggedInAsync();
        Assert.False(isLoggedIn);
    }

    [Fact]
    public async Task Login_WithMultipleFailedAttempts_ShouldLockoutUser()
    {
        // Arrange - Create a confirmed user
        var email = $"login-multiple-failures-{Guid.NewGuid()}@example.com";
        var password = "Test1234!";
        var displayName = "Lockout Attempts Test User";
        
        await CreateConfirmedUserAsync(email, password, displayName);
        
        // Configure user lockout (may already be configured in app)
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            
            // Enable lockout for this user
            await userManager.SetLockoutEnabledAsync(user!, true);
        }
        
        // Act - Make multiple failed login attempts
        var formData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "WrongPassword1!",  // Incorrect password
            ["RememberMe"] = "false",
            ["ReturnUrl"] = BasicReturnUrl
        };

        HttpResponseMessage? finalResponse = null;
        
        // Attempt multiple failed logins (default is 5 in ASP.NET Identity)
        for (int i = 0; i < 6; i++)
        {
            finalResponse = await PostFormWithAntiforgeryTokenAsync("/Account/Login", formData);
            if (finalResponse.StatusCode == HttpStatusCode.Redirect)
            {
                // User is locked out
                break;
            }
        }

        // Assert
        Assert.NotNull(finalResponse);
        Assert.Equal(HttpStatusCode.Redirect, finalResponse!.StatusCode);
        var redirectUrl = ExtractLocationHeader(finalResponse);
        Assert.Contains("/Account/LockedOut", redirectUrl);
        
        // Verify user is locked out
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            
            var isLockedOut = await userManager.IsLockedOutAsync(user);
            Assert.True(isLockedOut);
        }
    }

    private async Task CreateConfirmedUserAsync(string email, string password, string displayName)
    {
        using var scope = Factory.Services.CreateScope();
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
}
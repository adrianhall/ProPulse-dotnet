using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ProPulse.IdentityService.Models;
using Propulse.IdentityService.Tests.Infrastructure;

namespace Propulse.IdentityService.Tests.IntegrationTests;

public class PasswordResetIntegrationTests(IdentityServiceWebApplicationFactory factory) 
    : IdentityServiceIntegrationTestBase(factory)
{
    [Fact]
    public async Task ForgotPassword_WithValidEmail_ShouldSendResetLink()
    {
        // Arrange - Create a confirmed user
        var email = $"forgot-pwd-{Guid.NewGuid()}@example.com";
        var password = "Test1234!";
        var displayName = "Password Reset Test User";
        
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
        
        // Act - Request password reset
        var formData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        var response = await PostFormWithAntiforgeryTokenAsync("/Account/ForgotPassword", formData);
        
        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var redirectUrl = ExtractLocationHeader(response);
        Assert.Contains("/Account/AwaitPasswordReset", redirectUrl);
        
        // Verify password reset email was sent
        var resetEmail = EmailSender.GetLatestEmailByType(EmailType.PasswordResetLink);
        Assert.NotNull(resetEmail);
        Assert.Equal(email, resetEmail.Email);
        Assert.Contains("/Account/ResetPassword", resetEmail.Content);
    }

    [Fact]
    public async Task ForgotPassword_WithNonExistentEmail_ShouldStillRedirect()
    {
        // Arrange
        var nonExistentEmail = $"nonexistent-{Guid.NewGuid()}@example.com";
        
        // Act - Request password reset for non-existent email
        var formData = new Dictionary<string, string>
        {
            ["Email"] = nonExistentEmail,
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        var response = await PostFormWithAntiforgeryTokenAsync("/Account/ForgotPassword", formData);
        
        // Assert - Should still redirect to prevent user enumeration attacks
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var redirectUrl = ExtractLocationHeader(response);
        Assert.Contains("/Account/AwaitPasswordReset", redirectUrl);
        
        // No email should be sent since user doesn't exist
        var resetEmail = EmailSender.GetLatestEmailByType(EmailType.PasswordResetLink);
        Assert.Null(resetEmail);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ShouldResetPassword()
    {
        // Arrange - Create a user and request password reset
        var email = $"reset-valid-{Guid.NewGuid()}@example.com";
        var oldPassword = "OldPassword1!";
        var newPassword = "NewPassword1!";
        var displayName = "Password Reset Test User";
        
        // Create user
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
            
            await userManager.CreateAsync(user, oldPassword);
        }
        
        // Request password reset
        var forgotPasswordFormData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        await PostFormWithAntiforgeryTokenAsync("/Account/ForgotPassword", forgotPasswordFormData);
        
        // Get reset token from email
        var resetEmail = EmailSender.GetLatestEmailByType(EmailType.PasswordResetLink);
        Assert.NotNull(resetEmail);
        
        var resetLink = resetEmail.Content;
        var uri = new Uri(resetLink);
        var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var userId = queryParams["userId"] ?? string.Empty;
        var code = queryParams["code"] ?? string.Empty;
        
        // Visit reset password page to get token form
        var resetPageResponse = await Client.GetAsync($"/Account/ResetPassword?userId={userId}&code={code}");
        var resetPageContent = await resetPageResponse.Content.ReadAsStringAsync();
        
        // Act - Reset password
        var resetPasswordFormData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = newPassword,
            ["ConfirmPassword"] = newPassword,
            ["Token"] = code,
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        var response = await PostFormWithAntiforgeryTokenAsync("/Account/ResetPassword", resetPasswordFormData);
        
        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        
        // Verify new password works by trying to login
        // First, logout if already logged in
        await Client.GetAsync("/Account/Logout");
        
        var loginFormData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = newPassword,
            ["RememberMe"] = "false",
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        var loginResponse = await PostFormWithAntiforgeryTokenAsync("/Account/Login", loginFormData);
        
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        
        // Verify user is logged in
        var isLoggedIn = await IsUserLoggedInAsync();
        Assert.True(isLoggedIn);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ShouldReturnError()
    {
        // Arrange - Create a confirmed user
        var email = $"reset-invalid-{Guid.NewGuid()}@example.com";
        var oldPassword = "OldPassword1!";
        var newPassword = "NewPassword1!";
        var displayName = "Invalid Reset Test User";
        
        // Create user
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
            
            await userManager.CreateAsync(user, oldPassword);
        }
        
        // Act - Reset password with invalid token
        var resetPasswordFormData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = newPassword,
            ["ConfirmPassword"] = newPassword,
            ["Token"] = "InvalidToken",
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        var response = await PostFormWithAntiforgeryTokenAsync("/Account/ResetPassword", resetPasswordFormData);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid token", content);
        
        // Verify old password still works by trying to login
        var loginFormData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = oldPassword,
            ["RememberMe"] = "false",
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        var loginResponse = await PostFormWithAntiforgeryTokenAsync("/Account/Login", loginFormData);
        
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithPasswordMismatch_ShouldReturnError()
    {
        // Arrange - Create a user and request password reset
        var email = $"reset-mismatch-{Guid.NewGuid()}@example.com";
        var oldPassword = "OldPassword1!";
        var newPassword = "NewPassword1!";
        var displayName = "Password Mismatch Test User";
        
        // Create user
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
            
            await userManager.CreateAsync(user, oldPassword);
        }
        
        // Request password reset
        var forgotPasswordFormData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        await PostFormWithAntiforgeryTokenAsync("/Account/ForgotPassword", forgotPasswordFormData);
        
        // Get reset token from email
        var resetEmail = EmailSender.GetLatestEmailByType(EmailType.PasswordResetLink);
        Assert.NotNull(resetEmail);
        
        var resetLink = resetEmail.Content;
        var uri = new Uri(resetLink);
        var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var code = queryParams["code"] ?? string.Empty;
        
        // Act - Reset password with mismatched passwords
        var resetPasswordFormData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = newPassword,
            ["ConfirmPassword"] = "DifferentPassword1!",
            ["Token"] = code,
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        var response = await PostFormWithAntiforgeryTokenAsync("/Account/ResetPassword", resetPasswordFormData);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Password mismatch error message may vary, but should contain "password" and "match"
        Assert.Contains("password", content.ToLowerInvariant());
        Assert.Contains("match", content.ToLowerInvariant());
    }

    [Fact]
    public async Task EndToEnd_PasswordReset_ShouldAllowLogin()
    {
        // Arrange
        var email = $"e2e-reset-{Guid.NewGuid()}@example.com";
        var oldPassword = "OldPassword1!";
        var newPassword = "NewPassword1!";
        var displayName = "E2E Reset Test User";
        
        // Step 1: Create user
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
            
            await userManager.CreateAsync(user, oldPassword);
        }
        
        // Step 2: Request password reset
        var forgotPasswordFormData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        await PostFormWithAntiforgeryTokenAsync("/Account/ForgotPassword", forgotPasswordFormData);
        
        // Step 3: Get reset token from email
        var resetEmail = EmailSender.GetLatestEmailByType(EmailType.PasswordResetLink);
        Assert.NotNull(resetEmail);
        
        var resetLink = resetEmail.Content;
        var uri = new Uri(resetLink);
        var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var userId = queryParams["userId"] ?? string.Empty;
        var code = queryParams["code"] ?? string.Empty;
        
        // Step 4: Reset password
        var resetPasswordFormData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = newPassword,
            ["ConfirmPassword"] = newPassword,
            ["Token"] = code,
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        var resetResponse = await PostFormWithAntiforgeryTokenAsync("/Account/ResetPassword", resetPasswordFormData);
        
        // Step 5: Attempt login with new password
        var loginFormData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = newPassword,
            ["RememberMe"] = "false",
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        var loginResponse = await PostFormWithAntiforgeryTokenAsync("/Account/Login", loginFormData);
        
        // Assert final state
        Assert.Equal(HttpStatusCode.Redirect, resetResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        
        // Verify user is logged in with new password
        var isLoggedIn = await IsUserLoggedInAsync();
        Assert.True(isLoggedIn);
        
        // Verify old password no longer works
        await Client.GetAsync("/Account/Logout");
        
        var oldPasswordFormData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = oldPassword,
            ["RememberMe"] = "false",
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        var oldPasswordResponse = await PostFormWithAntiforgeryTokenAsync("/Account/Login", oldPasswordFormData);
        var content = await oldPasswordResponse.Content.ReadAsStringAsync();
        
        Assert.Equal(HttpStatusCode.OK, oldPasswordResponse.StatusCode);
        Assert.Contains("Invalid login attempt", content);
    }
}
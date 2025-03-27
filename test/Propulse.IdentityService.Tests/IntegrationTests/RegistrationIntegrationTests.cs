using System.Net;
using Propulse.IdentityService.Tests.Infrastructure;

namespace Propulse.IdentityService.Tests.IntegrationTests;

public class RegistrationIntegrationTests(IdentityServiceWebApplicationFactory factory) 
    : IdentityServiceIntegrationTestBase(factory)
{
    [Fact]
    public async Task Register_WithValidData_ShouldCreateUser_AndSendConfirmationEmail()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid()}@example.com";
        var password = "Test1234!";
        var displayName = "Test User";
        
        var formData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["ConfirmPassword"] = password,
            ["DisplayName"] = displayName,
            ["ReturnUrl"] = BasicReturnUrl
        };

        // Act
        var response = await PostFormWithAntiforgeryTokenAsync("/Account/Register", formData);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        
        // Validate user was created in database
        var user = await GetUserByEmailAsync(email);
        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
        Assert.Equal(displayName, user.DisplayName);
        
        // Validate confirmation email was sent
        var confirmationEmail = EmailSender.GetLatestEmailByType(EmailType.ConfirmationLink);
        Assert.NotNull(confirmationEmail);
        Assert.Equal(email, confirmationEmail.Email);
        Assert.Contains("/Account/ConfirmEmail", confirmationEmail.Content);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ShouldReturnError()
    {
        // Arrange
        var email = $"test-existing-{Guid.NewGuid()}@example.com";
        var password = "Test1234!";
        var displayName = "Test User";
        
        // Register first user
        var formData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["ConfirmPassword"] = password,
            ["DisplayName"] = displayName,
            ["ReturnUrl"] = BasicReturnUrl
        };
        await PostFormWithAntiforgeryTokenAsync("/Account/Register", formData);
        
        // Try to register another user with the same email
        // Act
        var response = await PostFormWithAntiforgeryTokenAsync("/Account/Register", formData);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Username", content);  // ASP.NET Identity uses username for duplicate message
        Assert.Contains("already taken", content);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturnError()
    {
        // Arrange
        var email = $"test-weak-{Guid.NewGuid()}@example.com";
        var password = "weak";  // Too short and simple
        var displayName = "Test User";
        
        var formData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["ConfirmPassword"] = password,
            ["DisplayName"] = displayName,
            ["ReturnUrl"] = BasicReturnUrl
        };

        // Act
        var response = await PostFormWithAntiforgeryTokenAsync("/Account/Register", formData);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Passwords must be at least", content);  // Default ASP.NET Identity message
    }

    [Fact]
    public async Task Register_WithPasswordMismatch_ShouldReturnError()
    {
        // Arrange
        var email = $"test-mismatch-{Guid.NewGuid()}@example.com";
        var password = "Test1234!";
        var displayName = "Test User";
        
        var formData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["ConfirmPassword"] = "DifferentPassword1!",
            ["DisplayName"] = displayName,
            ["ReturnUrl"] = BasicReturnUrl
        };

        // Act
        var response = await PostFormWithAntiforgeryTokenAsync("/Account/Register", formData);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("password", content);
        Assert.Contains("confirmation password", content);
    }

    [Fact]
    public async Task ConfirmEmail_WithValidToken_ShouldConfirmEmail()
    {
        // Arrange - Register a new user
        var email = $"confirm-{Guid.NewGuid()}@example.com";
        var password = "Test1234!";
        var displayName = "Test User";
        
        var formData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["ConfirmPassword"] = password,
            ["DisplayName"] = displayName,
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        await PostFormWithAntiforgeryTokenAsync("/Account/Register", formData);
        
        // Get confirmation link from email
        var confirmationEmail = EmailSender.GetLatestEmailByType(EmailType.ConfirmationLink);
        Assert.NotNull(confirmationEmail);
        
        var confirmationLink = confirmationEmail.Content;
        var uri = new Uri(confirmationLink);
        
        // Act - Visit the confirmation link
        var response = await Client.GetAsync(uri.PathAndQuery);
        
        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        
        // Verify user's email is confirmed
        var user = await GetUserByEmailAsync(email);
        Assert.NotNull(user);
        Assert.True(user.EmailConfirmed);
        
        // Verify the user is now logged in
        var isLoggedIn = await IsUserLoggedInAsync();
        Assert.True(isLoggedIn);
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_ShouldRedirectToResendConfirmation()
    {
        // Arrange
        var email = $"confirm-invalid-{Guid.NewGuid()}@example.com";
        var password = "Test1234!";
        var displayName = "Test User";
        
        // Register user
        var formData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["ConfirmPassword"] = password,
            ["DisplayName"] = displayName,
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        await PostFormWithAntiforgeryTokenAsync("/Account/Register", formData);
        var user = await GetUserByEmailAsync(email);
        Assert.NotNull(user);
        
        // Act - Visit confirmation link with invalid token
        var invalidConfirmationUrl = $"/Account/ConfirmEmail?userId={user.Id}&code=InvalidToken";
        var response = await Client.GetAsync(invalidConfirmationUrl);
        
        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var redirectUrl = ExtractLocationHeader(response);
        Assert.Contains("/Account/ResendEmailConfirmation", redirectUrl);
        
        // Verify user's email is still not confirmed
        user = await GetUserByEmailAsync(email);
        Assert.NotNull(user);
        Assert.False(user.EmailConfirmed);
    }

    [Fact]
    public async Task ResendConfirmationEmail_WithValidEmail_ShouldSendNewConfirmationEmail()
    {
        // Arrange - Register a user but clear the emails
        var email = $"resend-{Guid.NewGuid()}@example.com";
        var password = "Test1234!";
        var displayName = "Test User";
        
        var formData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["ConfirmPassword"] = password,
            ["DisplayName"] = displayName,
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        await PostFormWithAntiforgeryTokenAsync("/Account/Register", formData);
        
        // Clear emails to simulate user not receiving the first email
        EmailSender.ClearSentEmails();
        
        // Act - Resend confirmation email
        var resendFormData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        var response = await PostFormWithAntiforgeryTokenAsync("/Account/ResendEmailConfirmation", resendFormData);
        
        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var redirectUrl = ExtractLocationHeader(response);
        Assert.Contains("/Account/AwaitEmailConfirmation", redirectUrl);
        
        // Verify a new confirmation email was sent
        var confirmationEmail = EmailSender.GetLatestEmailByType(EmailType.ConfirmationLink);
        Assert.NotNull(confirmationEmail);
        Assert.Equal(email, confirmationEmail.Email);
        Assert.Contains("/Account/ConfirmEmail", confirmationEmail.Content);
    }

    [Fact]
    public async Task EndToEnd_RegisterAndConfirm_ShouldLoginUser()
    {
        // Arrange
        var email = $"e2e-{Guid.NewGuid()}@example.com";
        var password = "Test1234!";
        var displayName = "E2E Test User";
        
        // Step 1: Register user
        var registerFormData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["ConfirmPassword"] = password,
            ["DisplayName"] = displayName,
            ["ReturnUrl"] = BasicReturnUrl
        };
        
        await PostFormWithAntiforgeryTokenAsync("/Account/Register", registerFormData);
        
        // Step 2: Get confirmation email
        var confirmationEmail = EmailSender.GetLatestEmailByType(EmailType.ConfirmationLink);
        Assert.NotNull(confirmationEmail);
        
        // Step 3: Extract userId and code from the confirmation link
        var confirmationLink = confirmationEmail.Content;
        var userId = EmailSender.ExtractUserIdFromConfirmationLink(confirmationLink);
        var code = EmailSender.ExtractTokenFromConfirmationLink(confirmationLink);
        
        Assert.NotEmpty(userId);
        Assert.NotEmpty(code);
        
        // Step 4: Visit confirmation link
        var confirmationUrl = $"/Account/ConfirmEmail?userId={userId}&code={code}";
        var confirmResponse = await Client.GetAsync(confirmationUrl);
        
        // Assert final state
        Assert.Equal(HttpStatusCode.Redirect, confirmResponse.StatusCode);
        
        // Verify user is confirmed and logged in
        var user = await GetUserByEmailAsync(email);
        Assert.NotNull(user);
        Assert.True(user.EmailConfirmed);
        
        var isLoggedIn = await IsUserLoggedInAsync();
        Assert.True(isLoggedIn);
    }
}
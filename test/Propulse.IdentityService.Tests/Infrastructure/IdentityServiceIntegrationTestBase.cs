using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ProPulse.IdentityService.Data;
using ProPulse.IdentityService.Models;
using System.Net;
using System.Net.Http.Headers;

namespace Propulse.IdentityService.Tests.Infrastructure;

public abstract class IdentityServiceIntegrationTestBase : IClassFixture<IdentityServiceWebApplicationFactory>, IDisposable
{
    protected readonly IdentityServiceWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected readonly string BasicReturnUrl = "/";
    protected readonly FakeEmailSender EmailSender;

    protected IdentityServiceIntegrationTestBase(IdentityServiceWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        EmailSender = factory.EmailSender;
        EmailSender.ClearSentEmails();
    }

    protected async Task<(string AntiforgeryToken, string Cookie)> ExtractAntiforgeryTokenAsync(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        
        var cookie = response.Headers.GetValues("Set-Cookie").FirstOrDefault() ?? string.Empty;
        var responseContent = await response.Content.ReadAsStringAsync();
        
        var tokenMatch = System.Text.RegularExpressions.Regex.Match(responseContent, 
            @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        
        if (!tokenMatch.Success)
        {
            throw new Exception("Antiforgery token not found");
        }
        
        return (tokenMatch.Groups[1].Value, cookie);
    }

    protected async Task<HttpResponseMessage> PostFormWithAntiforgeryTokenAsync(
        string url, 
        Dictionary<string, string> formData)
    {
        var (token, cookie) = await ExtractAntiforgeryTokenAsync(url);
        
        formData["__RequestVerificationToken"] = token;
        
        var content = new FormUrlEncodedContent(formData);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        
        request.Headers.Add("Cookie", cookie);
        
        return await Client.SendAsync(request);
    }

    protected async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await userManager.FindByEmailAsync(email);
    }

    protected async Task<bool> IsUserLoggedInAsync()
    {
        var response = await Client.GetAsync("/Manage/Index");
        return response.StatusCode != HttpStatusCode.Unauthorized && 
               response.StatusCode != HttpStatusCode.Redirect;
    }

    protected string ExtractLocationHeader(HttpResponseMessage response)
    {
        if (response.Headers.Location == null)
        {
            throw new Exception("Location header not found");
        }
        return response.Headers.Location.ToString();
    }

    protected async Task SetUserEmailConfirmedAsync(string email, bool confirmed)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        
        if (user != null)
        {
            user.EmailConfirmed = confirmed;
            await userManager.UpdateAsync(user);
        }
    }

    protected async Task LoginUserAsync(string email, string password)
    {
        var formData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["RememberMe"] = "false",
            ["ReturnUrl"] = BasicReturnUrl
        };

        var response = await PostFormWithAntiforgeryTokenAsync("/Account/Login", formData);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
    }
}
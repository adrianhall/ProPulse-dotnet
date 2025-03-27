using Microsoft.AspNetCore.Identity;
using ProPulse.IdentityService.Models;
using System.Web;

namespace Propulse.IdentityService.Tests.Infrastructure;

public class FakeEmailSender : IEmailSender<ApplicationUser>
{
    private readonly List<EmailMessage> _sentEmails = new();

    public IReadOnlyList<EmailMessage> SentEmails => _sentEmails.AsReadOnly();

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        _sentEmails.Add(new EmailMessage
        {
            Type = EmailType.ConfirmationLink,
            Email = email,
            User = user,
            Content = confirmationLink
        });
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        _sentEmails.Add(new EmailMessage
        {
            Type = EmailType.PasswordResetCode,
            Email = email,
            User = user,
            Content = resetCode
        });
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        _sentEmails.Add(new EmailMessage
        {
            Type = EmailType.PasswordResetLink,
            Email = email,
            User = user,
            Content = resetLink
        });
        return Task.CompletedTask;
    }

    public void ClearSentEmails()
    {
        _sentEmails.Clear();
    }

    public EmailMessage? GetLatestEmailByType(EmailType type)
    {
        return _sentEmails.LastOrDefault(e => e.Type == type);
    }

    public string ExtractTokenFromConfirmationLink(string link)
    {
        // Example link format: https://localhost:5001/Account/ConfirmEmail?userId=abc&code=xyz
        var uri = new Uri(link);
        var queryParams = HttpUtility.ParseQueryString(uri.Query);
        return queryParams["code"] ?? string.Empty;
    }

    public string ExtractUserIdFromConfirmationLink(string link)
    {
        var uri = new Uri(link);
        var queryParams = HttpUtility.ParseQueryString(uri.Query);
        return queryParams["userId"] ?? string.Empty;
    }
}

public enum EmailType
{
    ConfirmationLink,
    PasswordResetCode,
    PasswordResetLink
}

public class EmailMessage
{
    public required EmailType Type { get; init; }
    public required string Email { get; init; }
    public required ApplicationUser User { get; init; }
    public required string Content { get; init; }
}
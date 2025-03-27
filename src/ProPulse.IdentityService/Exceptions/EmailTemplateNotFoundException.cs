namespace ProPulse.IdentityService.Exceptions;

internal class EmailTemplateNotFoundException : ApplicationException
{
    public EmailTemplateNotFoundException()
    {
    }

    public EmailTemplateNotFoundException(string? message) : base(message)
    {
    }

    public EmailTemplateNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public required IList<string> SearchedLocations { get; init; }
}
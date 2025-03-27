using System.ComponentModel.DataAnnotations;

namespace ProPulse.IdentityService.ViewModels;

public record EmailConfirmationInputModel
{
    public EmailConfirmationInputModel()
    {
    }

    public EmailConfirmationInputModel(EmailConfirmationInputModel model)
    {
        Email = model.Email;
        ReturnUrl = model.ReturnUrl;
    }

    [Required, EmailAddress, StringLength(256, MinimumLength = 3)]
    public string Email { get; set; } = string.Empty;

    public string ReturnUrl { get; set; } = string.Empty;
}

public record EmailConfirmationViewModel : EmailConfirmationInputModel
{
    public EmailConfirmationViewModel() : base()
    {
    }

    public EmailConfirmationViewModel(EmailConfirmationInputModel model) : base(model)
    {
    }
}

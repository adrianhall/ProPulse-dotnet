using System.ComponentModel.DataAnnotations;

namespace ProPulse.IdentityService.ViewModels;

public record RegisterExternalLoginInputModel
{
    public RegisterExternalLoginInputModel()
    {
    }

    public RegisterExternalLoginInputModel(RegisterExternalLoginInputModel model)
    {
        Email = model.Email;
        DisplayName = model.DisplayName;
        ReturnUrl = model.ReturnUrl;
    }

    [Required, EmailAddress, StringLength(256, MinimumLength = 3)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 1)]
    [RegularExpression(@"^[\w'\-,.]*[^_!¡?÷?¿\/\\+=@#$%ˆ&*(){}|~<>;:[\]]*$")]
    public string DisplayName { get; set; } = string.Empty;

    public string ReturnUrl { get; set; } = string.Empty;
}

public record RegisterExternalLoginViewModel : RegisterExternalLoginInputModel
{
    public RegisterExternalLoginViewModel() : base()
    {
    }

    public RegisterExternalLoginViewModel(RegisterExternalLoginInputModel model) : base(model)
    {
    }

    public string? ProviderDisplayName { get; set; }
}

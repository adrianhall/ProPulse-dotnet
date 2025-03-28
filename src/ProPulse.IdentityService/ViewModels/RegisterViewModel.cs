using Microsoft.AspNetCore.Authentication;
using System.ComponentModel.DataAnnotations;

namespace ProPulse.IdentityService.ViewModels;

public record RegisterInputModel
{
    public RegisterInputModel()
    {
    }

    public RegisterInputModel(RegisterInputModel inputModel)
    {
        Email = inputModel.Email;
        Password = inputModel.Password;
        ConfirmPassword = inputModel.ConfirmPassword;
        DisplayName = inputModel.DisplayName;
        ReturnUrl = inputModel.ReturnUrl;
    }

    [Required, EmailAddress, StringLength(256, MinimumLength = 3)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(64, MinimumLength = 3)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(64, MinimumLength = 3)]
    [Compare(nameof(Password), ErrorMessage = "Password and confirmation must match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 1)]
    [RegularExpression(@"^[\w'\-,.]*[^_!¡?÷?¿\/\\+=@#$%ˆ&*(){}|~<>;:[\]]*$")]
    public string DisplayName { get; set; } = string.Empty;

    public string ReturnUrl { get; set; } = string.Empty;
}

public record RegisterViewModel : RegisterInputModel
{
    public RegisterViewModel()
    {
    }

    public RegisterViewModel(RegisterInputModel inputModel) : base(inputModel)
    {
    }

    public IList<AuthenticationScheme> ExternalProviders { get; set; } = [];
}

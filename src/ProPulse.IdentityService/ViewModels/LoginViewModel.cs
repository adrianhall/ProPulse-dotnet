using Microsoft.AspNetCore.Authentication;
using System.ComponentModel.DataAnnotations;

namespace ProPulse.IdentityService.ViewModels;

public record LoginInputModel
{
    public LoginInputModel()
    {
    }

    public LoginInputModel(LoginInputModel model)
    {
        Email = model.Email;
        Password = model.Password;
        RememberMe = model.RememberMe;
        ReturnUrl = model.ReturnUrl;
    }

    [Required, EmailAddress, StringLength(256, MinimumLength = 3)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(64, MinimumLength = 3)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; } = true;

    public string ReturnUrl { get; set; } = string.Empty;
}

public record LoginViewModel : LoginInputModel
{
    public LoginViewModel()
    {
    }

    public LoginViewModel(LoginInputModel inputModel) : base(inputModel)
    {
    }

    public IList<AuthenticationScheme> ExternalProviders { get; set; } = [];
}

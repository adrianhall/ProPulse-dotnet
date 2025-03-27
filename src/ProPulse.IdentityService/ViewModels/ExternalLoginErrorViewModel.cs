using System.ComponentModel.DataAnnotations;

namespace ProPulse.IdentityService.ViewModels;

public class ExternalLoginErrorViewModel
{
    [Required, StringLength(256, MinimumLength = 1)]
    public string? ErrorMessage { get; set; }
}

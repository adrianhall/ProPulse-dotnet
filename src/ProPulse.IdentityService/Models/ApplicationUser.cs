using Microsoft.AspNetCore.Identity;

namespace ProPulse.IdentityService.Models;

public class ApplicationUser : IdentityUser
{
    [PersonalData]
    public string DisplayName { get; set; } = string.Empty;
}
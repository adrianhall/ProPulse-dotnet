using System.ComponentModel.DataAnnotations;

namespace ProPulse.IdentityService.ViewModels.ManageViewModels;

public class UserViewModel
{
    public string Id { get; set; } = string.Empty;
    
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;
    
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;
    
    [Display(Name = "Email Confirmed")]
    public bool EmailConfirmed { get; set; }
    
    [Display(Name = "Roles")]
    public List<string> Roles { get; set; } = new List<string>();
}
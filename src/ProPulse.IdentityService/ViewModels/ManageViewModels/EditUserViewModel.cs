using System.ComponentModel.DataAnnotations;

namespace ProPulse.IdentityService.ViewModels.ManageViewModels;

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;
    
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;
    
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;
    
    [Display(Name = "Email Confirmed")]
    public bool EmailConfirmed { get; set; }
    
    public List<RoleViewModel> AvailableRoles { get; set; } = new List<RoleViewModel>();
    public List<string> SelectedRoles { get; set; } = new List<string>();
}
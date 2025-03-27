using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProPulse.IdentityService.Models;
using ProPulse.IdentityService.ViewModels.ManageViewModels;
using System.Security.Claims;

namespace ProPulse.IdentityService.Controllers;

[Authorize(Roles = "Administrator")]
[Route("[controller]/[action]")]
public class ManageController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<ManageController> _logger;

    public ManageController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<ManageController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    // Helper method to get the current user ID
    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }

    // Helper method to check if user is editing themselves
    private bool IsCurrentUser(string userId)
    {
        var currentUserId = GetCurrentUserId();
        return !string.IsNullOrEmpty(currentUserId) && currentUserId == userId;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string searchString = "")
    {
        var usersQuery = _userManager.Users.AsQueryable();

        // Apply search filter if provided
        if (!string.IsNullOrEmpty(searchString))
        {
            usersQuery = usersQuery.Where(u => 
                (u.UserName != null && u.UserName.Contains(searchString)) || 
                (u.Email != null && u.Email.Contains(searchString)) || 
                u.DisplayName.Contains(searchString));
        }

        // Create paginated list
        var users = PaginatedList<ApplicationUser>.Create(
            usersQuery.OrderBy(u => u.UserName), 
            pageNumber, 
            pageSize);

        // Map to view models
        var userViewModels = new List<UserViewModel>();
        
        foreach (var user in users.Items)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userViewModels.Add(new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList()
            });
        }

        var model = new PaginatedList<UserViewModel>(
            userViewModels, 
            users.TotalItems, 
            users.PageIndex, 
            pageSize);

        ViewBag.SearchString = searchString;
        return View(model);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var availableRoles = await _roleManager.Roles
            .Select(r => new RoleViewModel 
            { 
                Id = r.Id, 
                Name = r.Name ?? string.Empty,
                IsSelected = userRoles.Contains(r.Name ?? string.Empty)
            })
            .ToListAsync();

        // If this is the current user, disable ability to remove Administrator role
        if (IsCurrentUser(id))
        {
            foreach (var role in availableRoles)
            {
                if (role.Name == "Administrator" && role.IsSelected)
                {
                    role.IsSelected = true;
                    ViewBag.IsCurrentUser = true;
                }
            }
        }

        var model = new EditUserViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            EmailConfirmed = user.EmailConfirmed,
            AvailableRoles = availableRoles,
            SelectedRoles = userRoles.ToList()
        };

        // Add a flag to indicate if user is editing themselves
        ViewBag.IsCurrentUser = IsCurrentUser(id);

        return View(model);
    }

    [HttpPost("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditUserViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Check if user is editing themselves and trying to remove their Administrator role
        bool isCurrentUser = IsCurrentUser(id);
        if (isCurrentUser)
        {
            var existingRoles = await _userManager.GetRolesAsync(user);
            if (existingRoles.Contains("Administrator") && !model.SelectedRoles.Contains("Administrator"))
            {
                ModelState.AddModelError(string.Empty, "You cannot remove your own Administrator role.");
                
                var userRoles = await _userManager.GetRolesAsync(user);
                model.AvailableRoles = await _roleManager.Roles
                    .Select(r => new RoleViewModel 
                    { 
                        Id = r.Id, 
                        Name = r.Name ?? string.Empty,
                        IsSelected = userRoles.Contains(r.Name ?? string.Empty)
                    })
                    .ToListAsync();
                
                ViewBag.IsCurrentUser = true;
                return View(model);
            }
        }

        if (ModelState.IsValid)
        {
            // Update display name
            user.DisplayName = model.DisplayName;
            user.EmailConfirmed = model.EmailConfirmed;
            
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                
                // Repopulate roles for the view
                var userRoles = await _userManager.GetRolesAsync(user);
                model.AvailableRoles = await _roleManager.Roles
                    .Select(r => new RoleViewModel 
                    { 
                        Id = r.Id, 
                        Name = r.Name ?? string.Empty,
                        IsSelected = userRoles.Contains(r.Name ?? string.Empty)
                    })
                    .ToListAsync();
                
                ViewBag.IsCurrentUser = isCurrentUser;
                return View(model);
            }

            // Handle role updates
            var existingRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = existingRoles.Where(r => !model.SelectedRoles.Contains(r));
            var rolesToAdd = model.SelectedRoles.Where(r => !existingRoles.Contains(r));

            if (rolesToRemove.Any())
            {
                result = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    ViewBag.IsCurrentUser = isCurrentUser;
                    return View(model);
                }
            }

            if (rolesToAdd.Any())
            {
                result = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    ViewBag.IsCurrentUser = isCurrentUser;
                    return View(model);
                }
            }

            _logger.LogInformation("User {userId} was successfully updated by admin.", user.Id);
            TempData["StatusMessage"] = "User has been updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // If we got this far, something failed, redisplay form
        var currentRoles = await _userManager.GetRolesAsync(user);
        model.AvailableRoles = await _roleManager.Roles
            .Select(r => new RoleViewModel 
            { 
                Id = r.Id, 
                Name = r.Name ?? string.Empty,
                IsSelected = currentRoles.Contains(r.Name ?? string.Empty)
            })
            .ToListAsync();
        
        ViewBag.IsCurrentUser = isCurrentUser;
        return View(model);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Check if user is trying to delete themselves
        if (IsCurrentUser(id))
        {
            TempData["ErrorMessage"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        
        var model = new UserViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            EmailConfirmed = user.EmailConfirmed,
            Roles = userRoles.ToList()
        };

        return View(model);
    }

    [HttpPost("{id}")]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Check if user is trying to delete themselves
        if (IsCurrentUser(id))
        {
            TempData["ErrorMessage"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        // Don't allow deletion of the last admin
        var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");
        if (isAdmin)
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Administrator");
            if (adminUsers.Count <= 1)
            {
                ModelState.AddModelError(string.Empty, "Cannot delete the last administrator account.");
                
                var userRoles = await _userManager.GetRolesAsync(user);
                var model = new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    DisplayName = user.DisplayName,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = userRoles.ToList()
                };
                
                return View(model);
            }
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            _logger.LogInformation("User {userId} was deleted by admin.", user.Id);
            TempData["StatusMessage"] = "User has been deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        
        var currentRoles = await _userManager.GetRolesAsync(user);
        var viewModel = new UserViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            EmailConfirmed = user.EmailConfirmed,
            Roles = currentRoles.ToList()
        };
        
        return View(viewModel);
    }
}
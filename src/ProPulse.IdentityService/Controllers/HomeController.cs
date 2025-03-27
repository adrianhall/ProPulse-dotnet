using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProPulse.IdentityService.Models;
using ProPulse.IdentityService.ViewModels;
using System.Diagnostics;

namespace ProPulse.IdentityService.Controllers;

public class HomeController(
    ILogger<HomeController> logger,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager
) : Controller
{
    public async Task<IActionResult> Index()
    {
        // Check if the user is authenticated but doesn't exist in the database
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                logger.LogWarning("Authenticated user {UserId} not found in database. Signing out.", User.FindFirst("sub")?.Value);
                await signInManager.SignOutAsync();
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["CurrentUser"] = currentUser;
        }
        
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
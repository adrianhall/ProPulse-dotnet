using Microsoft.AspNetCore.Identity;
using ProPulse.IdentityService.Models;

namespace ProPulse.IdentityService.Services.Implementations;

public class DatabaseInitializationService(
    IServiceProvider serviceProvider,
    ILogger<DatabaseInitializationService> logger
    ) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Create a scope to resolve scoped services
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            logger.LogInformation("Starting database initialization service");

            // Get required services
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Create roles if they don't exist
            string[] roleNames = ["User", "Administrator", "Author"];
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    logger.LogInformation("Creating role: {RoleName}", roleName);
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Check if any users exist
            if (!userManager.Users.Any())
            {
                logger.LogInformation("No users found. Creating default administrator account");

                // Create default admin user
                var adminUser = new ApplicationUser
                {
                    UserName = "admin@propulse.local",
                    Email = "admin@propulse.local",
                    EmailConfirmed = true,
                    DisplayName = "System Administrator"
                };

                // Generate a random password
                var password = GenerateSecurePassword();
                
                var result = await userManager.CreateAsync(adminUser, password);
                if (result.Succeeded)
                {
                    // Add to Administrator role
                    await userManager.AddToRoleAsync(adminUser, "Administrator");
                    
                    // Log the admin credentials
                    logger.LogWarning(
                        "Default administrator account created. Please change the password immediately!\n" +
                        "Username: {Username}\n" +
                        "Password: {Password}", 
                        adminUser.Email, 
                        password);
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    logger.LogError("Failed to create admin user: {Errors}", errors);
                }
            }
            else
            {
                logger.LogInformation("Users already exist. Skipping default admin creation");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database initialization");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // No cleanup needed
        return Task.CompletedTask;
    }

    private string GenerateSecurePassword()
    {
        // Generate a secure random password (16 chars) with uppercase, lowercase, numbers, and special chars
        const string uppercaseChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowercaseChars = "abcdefghijkmnopqrstuvwxyz";
        const string numberChars = "23456789";
        const string specialChars = "!@#$%^&*()_-+=<>?";
        
        var random = new Random();
        var passwordChars = new char[16];
        
        // Ensure at least one of each character type
        passwordChars[0] = uppercaseChars[random.Next(uppercaseChars.Length)];
        passwordChars[1] = lowercaseChars[random.Next(lowercaseChars.Length)];
        passwordChars[2] = numberChars[random.Next(numberChars.Length)];
        passwordChars[3] = specialChars[random.Next(specialChars.Length)];
        
        // Fill the rest randomly
        var allChars = uppercaseChars + lowercaseChars + numberChars + specialChars;
        for (int i = 4; i < passwordChars.Length; i++)
        {
            passwordChars[i] = allChars[random.Next(allChars.Length)];
        }
        
        // Shuffle the array
        for (int i = 0; i < passwordChars.Length; i++)
        {
            int swapIndex = random.Next(passwordChars.Length);
            (passwordChars[i], passwordChars[swapIndex]) = (passwordChars[swapIndex], passwordChars[i]);
        }
        
        return new string(passwordChars);
    }
}
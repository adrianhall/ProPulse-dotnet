using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using ProPulse.IdentityService.Extensions;
using ProPulse.IdentityService.Models;
using ProPulse.IdentityService.ViewModels;
using System.Security.Claims;
using System.Text;

namespace ProPulse.IdentityService.Controllers;

/// <summary>
/// The controller that handles all the account-related transactions for ASP.NET Identity.
/// </summary>
[AutoValidateAntiforgeryToken]
public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IEmailSender<ApplicationUser> emailSender,
    IOptions<IdentityOptions> identityOptions,
    ILogger<AccountController> logger
    ) : Controller
{
    /// <summary>
    /// The page after email confirmation is sent.
    /// </summary>
    [HttpGet, AllowAnonymous]
    public IActionResult AwaitEmailConfirmation(string email, string returnUrl)
    {
        logger.LogTrace("GET AwaitEmailConfirmation: {email} {returnUrl}", email, returnUrl);
        return View(new EmailConfirmationViewModel { Email = email, ReturnUrl = returnUrl });
    }

    /// <summary>
    /// The page after a reset password link is sent.
    /// </summary>
    [HttpGet, AllowAnonymous]
    public IActionResult AwaitPasswordReset(string email, string returnUrl)
    {
        logger.LogTrace("GET AwaitPasswordResewt: {email} {returnUrl}", email, returnUrl);
        return View(new EmailConfirmationViewModel { Email = email, ReturnUrl = returnUrl });
    }

    /// <summary>
    /// Processes the emailed confirmation link.
    /// </summary>
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string? userId, [FromQuery] string? code)
    {
        logger.LogTrace("GET ConfirmEmail: {userId} {code}", userId, code);
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
        {
            logger.LogWarning("ConfirmEmail: UserId or code is null or empty.");
            return RedirectToHomePage();
        }

        ApplicationUser? user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            logger.LogWarning("ConfirmEmail: User {userId} not found.", userId);
            return RedirectToHomePage();
        }

        try
        {
            string token = DecodeToken(code);
            IdentityResult result = await userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                logger.LogWarning("ConfirmEmail: Could not confirm email for user {userId}: {errors}", userId, result.Errors);
                return RedirectToAction(
                    nameof(ResendEmailConfirmation),
                    new { user.Email }
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("ConfirmEmail: Could not confirm email for user {userId}: {exception}", userId, ex);
            return RedirectToAction(
                nameof(ResendEmailConfirmation),
                new { user.Email }
            );
        }

        // User has confirmed their email, so we can now sign them in.
        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToHomePage();
    }

    /// <summary>
    /// Initiation of a social login.
    /// </summary>
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> ExternalLogin([FromQuery] string? returnUrl, [FromQuery] string provider)
    {
        logger.LogTrace("GET ExternalLogin returnUrl={returnUrl}, provider={provider}", returnUrl, provider);
        returnUrl ??= Url.Content("~/");
        IList<AuthenticationScheme> authProviders = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        if (!authProviders.Any(x => x.Name.Equals(provider)))
        {
            // If the provider is not known, then just go back to the login page.
            logger.LogWarning("GET ExternalLogin provider={provider} unknown", provider);
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        string? redirectUrl = Url.ActionLink(nameof(ExternalLoginCallback), values: new { returnUrl });
        AuthenticationProperties properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        logger.LogTrace("Returning Challenge for provider={provider}, redirectUrl={redirectUrl}", provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    /// <summary>
    /// The call-back that the remote social login provider calls when their authentication is complete.
    /// </summary>
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        logger.LogTrace("GET ExternalLoginCallback returnUrl={returnUrl}, remoteError={remoteError}", returnUrl, remoteError);
        returnUrl ??= Url.Content("~/");
        if (remoteError is not null)
        {
            logger.LogWarning("GET ExternalLoginCallback: remoteError is not null - redirecting to error page");
            TempData["ErrorMessage"] = $"Error from external provider: {remoteError}";
            return RedirectToAction(nameof(ExternalLoginError));
        }

        ExternalLoginInfo? info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            logger.LogWarning("GET ExternalLoginCallback: cannot retrieve external login info - redirecting to error page");
            TempData["ErrorMessage"] = "Error loading external login information";
            return RedirectToAction(nameof(ExternalLoginError));
        }

        // Sign the user in with this external login provider if the user already has a login.
        var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            logger.LogInformation("{Name} logged in with {LoginProvider} provider", info.Principal.Identity?.Name, info.LoginProvider);
            return Redirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            logger.LogInformation("{Name} is locked out", info.Principal.Identity?.Name);
            return RedirectToAction(nameof(LockedOut));
        }

        logger.LogInformation("{Name} is not registered yet - registering", info.Principal.Identity?.Name);
        RegisterExternalLoginViewModel viewModel = new()
        {
            ReturnUrl = returnUrl,
            ProviderDisplayName = info.ProviderDisplayName,
            Email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty
        };
        return View(nameof(RegisterExternalLogin), viewModel);
    }

    /// <summary>
    /// End result page when the social login provider indicates there is an error.
    /// </summary>
    /// <returns></returns>
    [HttpGet, AllowAnonymous]
    public IActionResult ExternalLoginError()
    {
        ExternalLoginErrorViewModel viewModel = new() { ErrorMessage = TempData["ErrorMessage"]?.ToString() };
        return View(viewModel);
    }

    /// <summary>
    /// Displays the "forgot password" dialog.
    /// </summary>
    [HttpGet, AllowAnonymous]
    public IActionResult ForgotPassword([FromQuery] string? returnUrl)
    {
        logger.LogTrace("GET ForgotPassword: {returnUrl}", returnUrl);

        returnUrl = HomePageIfNullOrEmpty(returnUrl);
        if (signInManager.IsSignedIn(User))
        {
            logger.LogDebug("Login: User is already signed in.");
            return RedirectToHomePage();
        }

        EmailConfirmationViewModel viewModel = new()
        {
            ReturnUrl = returnUrl
        };

        return View(viewModel);
    }

    /// <summary>
    /// Form handler for the ForgotPassword form.
    /// </summary>
    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromForm] EmailConfirmationInputModel model)
    {
        logger.LogTrace("POST ForgotPassword: {json}", model.ToJsonString());

        model.ReturnUrl = HomePageIfNullOrEmpty(model.ReturnUrl);
        if (!ModelState.IsValid)
        {
            EmailConfirmationViewModel viewModel = new(model);
            LogAllModelStateErrors(ModelState);
            return View(viewModel);
        }

        ApplicationUser? user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            logger.LogDebug("ForgotPassword: User '{email}' not found.", model.Email);
            return RedirectToAction(nameof(AwaitPasswordReset), new { model.Email, model.ReturnUrl });
        }

        await SendResetPasswordLinkAsync(user);
        return RedirectToAction(
            nameof(AwaitPasswordReset),
            new { model.Email, model.ReturnUrl }
        );
    }

    /// <summary>
    /// Displays the locked out page to the user.
    /// </summary>
    [HttpGet, AllowAnonymous]
    public IActionResult LockedOut()
        => View();

    /// <summary>
    /// Displays the blank login page to the user.
    /// </summary>
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        logger.LogTrace("GET Login: {returnUrl}", returnUrl);

        returnUrl = HomePageIfNullOrEmpty(returnUrl);
        if (signInManager.IsSignedIn(User))
        {
            logger.LogDebug("Login: User is already signed in.");
            return RedirectToHomePage();
        }

        LoginViewModel viewModel = new()
        {
            ReturnUrl = returnUrl,
            ExternalProviders = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList(),
        };

        return View(viewModel);
    }

    /// <summary>
    /// Processes the login form submission.
    /// </summary>
    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Login([FromForm] LoginInputModel model)
    {
        logger.LogTrace("POST Login: {json}", model.ToJsonString());

        async Task<IActionResult> DisplayLoginView()
        {
            LoginViewModel viewModel = new LoginViewModel(model)
            {
                ExternalProviders = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList(),
            };
            return View(viewModel);
        }

        model.ReturnUrl = HomePageIfNullOrEmpty(model.ReturnUrl);
        if (!ModelState.IsValid)
        {
            LogAllModelStateErrors(ModelState);
            return await DisplayLoginView();
        }

        ApplicationUser? user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            logger.LogDebug("Login: User '{email}' not found.", model.Email);
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return await DisplayLoginView();
        }

        // Note: we overloads the AllowedForNewUsers option to enable/disable lockout for everyone.
        var result = await signInManager.PasswordSignInAsync(
            user, model.Password,
            isPersistent: model.RememberMe,
            lockoutOnFailure: identityOptions.Value.Lockout.AllowedForNewUsers
        );
        if (result.Succeeded)
        {
            logger.LogDebug("Login: User logged in.");
            return RedirectToHomePage();
        }

        if (result.IsLockedOut)
        {
            logger.LogDebug("Login: User '{email}' is locked out.", model.Email);
            return RedirectToAction(nameof(LockedOut));
        }

        logger.LogDebug("Login: Invalid username/password for user '{email}'.", model.Email);
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return await DisplayLoginView();
    }

    /// <summary>
    /// Logs the signed in user out.
    /// </summary>
    [HttpGet, Authorize]
    public async Task<IActionResult> Logout([FromQuery] string? returnUrl = null)
    {
        logger.LogTrace("GET Logout: {returnUrl}", returnUrl);

        returnUrl = HomePageIfNullOrEmpty(returnUrl);
        if (!signInManager.IsSignedIn(User))
        {
            logger.LogTrace("Logout: User is not signed in.");
            return Redirect(returnUrl);
        }

        ApplicationUser? user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            logger.LogWarning("Logout: User is signed in but not found.");
            await signInManager.SignOutAsync();
            return Redirect(returnUrl);
        }

        logger.LogDebug("Logout: User '{email}' signed out.", user.Email);
        await signInManager.SignOutAsync();
        return Redirect(returnUrl);
    }

    /// <summary>
    /// Displays the registration page.
    /// </summary>
    [HttpGet, AllowAnonymous]
    public IActionResult Register([FromQuery] string? returnUrl = null)
    {
        logger.LogTrace("GET Register: {returnUrl}", returnUrl);

        returnUrl = HomePageIfNullOrEmpty(returnUrl);
        if (signInManager.IsSignedIn(User))
        {
            logger.LogDebug("Register: User is already signed in.");
            return Redirect(returnUrl);
        }

        RegisterViewModel viewModel = new()
        {
            ReturnUrl = returnUrl
        };

        return View(viewModel);
    }

    /// <summary>
    /// Form processor for the registration page.
    /// </summary>
    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Register([FromForm] RegisterInputModel model)
    {
        logger.LogTrace("POST Register: {json}", model.ToJsonString());

        async Task<IActionResult> DisplayRegisterView()
        {
            RegisterViewModel viewModel = new RegisterViewModel(model)
            {
                ExternalProviders = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList(),
            };
            return View(viewModel);
        }

        model.ReturnUrl = HomePageIfNullOrEmpty(model.ReturnUrl);
        if (!ModelState.IsValid)
        {
            LogAllModelStateErrors(ModelState);
            return await DisplayRegisterView();
        }

        ApplicationUser user = new()
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName
        };

        logger.LogTrace("Register: Creating user {json}", user.ToJsonString());
        IdentityResult result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            logger.LogError("Register: Could not create user {json}: {errors}", user.ToJsonString(), result.Errors);
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return await DisplayRegisterView();
        }

        if (!userManager.Options.SignIn.RequireConfirmedAccount)
        {
            logger.LogDebug("Register: RequireConfirmedAccount = false; sign-in {email} automatically", user.Email);
            await signInManager.SignInAsync(user, isPersistent: false);
            return Redirect(model.ReturnUrl);
        }

        await SendConfirmationLinkAsync(user);
        return RedirectToAction(
            nameof(AwaitEmailConfirmation),
            new { model.Email, model.ReturnUrl }
        );
    }

    /// <summary>
    /// Register a user based on the identity model from a social login provider.
    /// </summary>
    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> RegisterExternalLogin([FromForm] RegisterExternalLoginInputModel model)
    {
        logger.LogTrace("POST RegisterExternalLogin {model}", model.ToJsonString());
        model.ReturnUrl = HomePageIfNullOrEmpty(model.ReturnUrl);
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            logger.LogWarning("GET ExternalLoginCallback: cannot retrieve external login info - redirecting to error page");
            TempData["ErrorMessage"] = "Error loading external login information";
            return RedirectToAction(nameof(ExternalLoginError));
        }

        IActionResult DisplayRegisterView()
        {
            RegisterExternalLoginViewModel viewModel = new(model) { ProviderDisplayName = info.ProviderDisplayName };
            return View(viewModel);
        }

        if (!ModelState.IsValid)
        {
            LogAllModelStateErrors(ModelState);
            return DisplayRegisterView();
        }

        ApplicationUser user = new()
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName
        };

        logger.LogTrace("RegisterExternalLogin: Creating user {json}", user.ToJsonString());
        IdentityResult result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            logger.LogError("RegisterExternalLogin: Could not create user {json}: {errors}", user.ToJsonString(), result.Errors);
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return DisplayRegisterView();
        }

        if (!userManager.Options.SignIn.RequireConfirmedAccount)
        {
            logger.LogDebug("RegisterExternalLogin: RequireConfirmedAccount = false; sign-in {email} automatically", user.Email);
            await signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
            return Redirect(model.ReturnUrl);
        }

        await SendConfirmationLinkAsync(user);
        return RedirectToAction(
            nameof(AwaitEmailConfirmation),
            new { model.Email, model.ReturnUrl }
        );
    }

    /// <summary>
    /// User is asking to resend the email confirmation from a registration request.
    /// </summary>
    [HttpGet, AllowAnonymous]
    public IActionResult ResendEmailConfirmation([FromQuery] string? email)
    {
        logger.LogTrace("GET ResendEmailConfirmation: {email}", email);
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("ResendEmailConfirmation: Email is null or empty.");
            return RedirectToHomePage();
        }

        return View(new EmailConfirmationViewModel { Email = email });
    }

    /// <summary>
    /// The form processor for the resend email confirmation page.
    /// </summary>
    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> ResendEmailConfirmation([FromForm] EmailConfirmationInputModel model)
    {
        logger.LogTrace("POST ResendEmailConfirmation: {json}", model.ToJsonString());

        model.ReturnUrl = HomePageIfNullOrEmpty(model.ReturnUrl);
        if (!ModelState.IsValid)
        {
            LogAllModelStateErrors(ModelState);
            return View(model);
        }

        ApplicationUser? user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            logger.LogWarning("ResendEmailConfirmation: User '{email}' not found.", model.Email);
            ModelState.AddModelError(string.Empty, "Invalid email address.");
            return View(model);
        }

        await SendConfirmationLinkAsync(user);
        return RedirectToAction(
            nameof(AwaitEmailConfirmation),
            new { model.Email, model.ReturnUrl }
        );
    }

    /// <summary>
    /// The user has clicked on the link inside the password reset email.
    /// </summary>
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromQuery] string? userId, [FromQuery] string? code)
    {
        logger.LogTrace("GET ResetPassword: {userId} {code}", userId, code);
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
        {
            logger.LogWarning("ResetPassword: UserId or code is null or empty.");
            return RedirectToHomePage();
        }

        ApplicationUser? user = await userManager.FindByIdAsync(userId);
        if (user is null || user.Email is null)
        {
            logger.LogWarning("ResetPassword: User {userId} not found.", userId);
            return RedirectToHomePage();
        }

        string resetToken = DecodeToken(code);
        ResetPasswordViewModel viewModel = new() { Email = user.Email, Token = resetToken };
        return View(viewModel);
    }

    /// <summary>
    /// Form processor for the reset password page.
    /// </summary>
    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromForm] ResetPasswordInputModel model)
    {
        logger.LogTrace("POST ResetPassword: {json}", model.ToJsonString());

        IActionResult DisplayView()
        {
            ResetPasswordViewModel viewModel = new(model);
            return View(viewModel);
        }

        model.ReturnUrl = HomePageIfNullOrEmpty(model.ReturnUrl);
        if (!ModelState.IsValid)
        {
            LogAllModelStateErrors(ModelState);
            return DisplayView();
        }

        ApplicationUser? user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            logger.LogWarning("ResetPassword: User '{email}' not found.", model.Email);
            ModelState.AddModelError(string.Empty, "Invalid email address.");
            return DisplayView();
        }

        IdentityResult result = await userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (!result.Succeeded)
        {
            logger.LogWarning("ResetPassword: Could not reset password for {email}:", model.Email);
            foreach (IdentityError error in result.Errors)
            {
                logger.LogWarning("Error: {errorDescription}", error.Description);
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return DisplayView();
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return Redirect(model.ReturnUrl);
    }

    #region Helpers
    /// <summary>
    /// Converts the return URL to the home page if it is null or empty.
    /// </summary>
    /// <param name="returnUrl">The return URL</param>
    /// <returns>The modeified return URL</returns>
    internal string HomePageIfNullOrEmpty(string? returnUrl)
        => string.IsNullOrWhiteSpace(returnUrl) ? Url.Content("~/") : returnUrl;

    /// <summary>
    /// Logs all the errors within a model state dictionary.
    /// </summary>
    /// <param name="modelState">The model state dictionary to log.</param>
    internal void LogAllModelStateErrors(ModelStateDictionary modelState)
    {
        foreach (ModelError error in modelState.Values.SelectMany(v => v.Errors))
        {
            logger.LogDebug("ModelState: {error}", error.ErrorMessage);
        }
    }

    /// <summary>
    /// Sends the registration confirmation link to the user.
    /// </summary>
    /// <param name="user">The user record.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the operation is completed.</returns>
    internal Task SendConfirmationLinkAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        logger.LogTrace("SendConfirmationLink: {json}", user.ToJsonString());
        return Task.Run(async () =>
        {
            string userId = await userManager.GetUserIdAsync(user);
            string token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            logger.LogTrace("SendConfirmationLink: {userId} {token}", userId, token);
            string? callbackUrl = Url.ActionLink(
                action: nameof(ConfirmEmail),
                values: new { userId, code = EncodeToken(token) },
                protocol: Request.Scheme
            );
            logger.LogTrace("SendConfirmationLink: {userId} {callbackUrl}", userId, callbackUrl);
            if (callbackUrl is null || user.Email is null)
            {
                logger.LogError("Failed to generate registration confirmation link for user {userId}", userId);
                throw new ApplicationException("Failed to generate registration confirmation link.");
            }
            await emailSender.SendConfirmationLinkAsync(user, user.Email, callbackUrl);
        }, cancellationToken);
    }

    /// <summary>
    /// Sends the registration confirmation link to the user.
    /// </summary>
    /// <param name="user">The user record.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the operation is completed.</returns>
    internal Task SendResetPasswordLinkAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        logger.LogTrace("SendResetPasswordLink: {json}", user.ToJsonString());
        return Task.Run(async () =>
        {
            string userId = await userManager.GetUserIdAsync(user);
            string token = await userManager.GeneratePasswordResetTokenAsync(user);
            logger.LogTrace("SendResetPasswordLink: {userId} {token}", userId, token);
            string? callbackUrl = Url.ActionLink(
                action: nameof(ResetPassword),
                values: new { userId, code = EncodeToken(token) },
                protocol: Request.Scheme
            );
            logger.LogTrace("SendResetPasswordLink: {userId} {callbackUrl}", userId, callbackUrl);
            if (callbackUrl is null || user.Email is null)
            {
                logger.LogError("Failed to generate password reset link for user {userId}", userId);
                throw new ApplicationException("Failed to generate password reset link.");
            }
            await emailSender.SendPasswordResetLinkAsync(user, user.Email, callbackUrl);
        }, cancellationToken);
    }

    /// <summary>
    /// Decodes an incoming token back to a form that can be validated by ASP.NET Identity.
    /// </summary>
    /// <param name="code">The code from the user.</param>
    /// <returns>The ASP.NET Identity token.</returns>
    internal static string DecodeToken(string code)
        => Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));

    /// <summary>
    /// Encodes a confirmation token to a form that can be sent to a user as a URL.
    /// </summary>
    /// <param name="token">The token to encode for the user.</param>
    /// <returns>The code to be sent to the user.</returns>
    internal static string EncodeToken(string token)
        => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    /// <summary>
    /// Returns an <see cref="IActionResult"/> that redirects the user to the home page.
    /// </summary>
    internal IActionResult RedirectToHomePage()
        => LocalRedirect(Url.Content("~/"));
    #endregion
}
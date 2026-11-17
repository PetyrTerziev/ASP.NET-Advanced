using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using GuardPost.Common;
using GuardPost.Common.Providers;
using GuardPost.Data.Models;
using GuardPost.Web.ViewModels.Account;

namespace GuardPost.Web.Controllers;

/// <summary>
/// Login, registration and logout on top of ASP.NET Core Identity
/// (own Bulgarian views instead of the default Identity UI).
/// </summary>
public class AccountController : BaseController
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly IDateTimeProvider clock;
    private readonly ILogger<AccountController> logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IDateTimeProvider clock,
        ILogger<AccountController> logger)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.clock = clock;
        this.logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
        => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, ValidationMessages.AccountLocked);
            return View(model);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, ValidationMessages.InvalidLogin);
            return View(model);
        }

        logger.LogInformation("User {Email} signed in.", model.Email);

        // Only local URLs are accepted – protects against open-redirect attacks.
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await userManager.FindByEmailAsync(model.Email) != null)
        {
            ModelState.AddModelError(nameof(model.Email), ValidationMessages.EmailTaken);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            CreatedOn = clock.Now,
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await userManager.AddToRoleAsync(user, RoleNames.User);
        await signInManager.SignInAsync(user, isPersistent: false);

        ShowSuccess("Регистрацията е успешна. Администратор ще свърже профила Ви с картона на охранител.");
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}

using Microsoft.AspNetCore.Mvc;

using GuardPost.Common;
using GuardPost.Services.Core.Contracts;
using GuardPost.Web.Infrastructure.Extensions;
using GuardPost.Web.ViewModels.Common;
using GuardPost.Web.ViewModels.Users;

namespace GuardPost.Web.Areas.Admin.Controllers;

public class UsersController : AdminBaseController
{
    private readonly IUserAdminService userAdminService;

    public UsersController(IUserAdminService userAdminService)
    {
        this.userAdminService = userAdminService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, int page = 1)
    {
        var model = await userAdminService.GetUsersAsync(searchTerm, page, ApplicationConstants.DefaultPageSize);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SetAdministrator(string id, bool value)
    {
        var result = await userAdminService.SetAdministratorAsync(id, value, User.GetUserId());
        ShowResult(result, value ? "Потребителят вече е администратор." : "Администраторските права бяха премахнати.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetLocked(string id, bool value)
    {
        var result = await userAdminService.SetLockedAsync(id, value, User.GetUserId());
        ShowResult(result, value ? "Профилът беше заключен." : "Профилът беше отключен.");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> LinkGuard(string id)
    {
        var model = await userAdminService.GetLinkGuardAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> LinkGuard(UserLinkGuardViewModel model)
    {
        var result = await userAdminService.LinkGuardAsync(model.UserId, model.GuardId);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(model.GuardId), result.Error!);

            var reloaded = await userAdminService.GetLinkGuardAsync(model.UserId);
            if (reloaded is null)
            {
                return NotFound();
            }

            reloaded.GuardId = model.GuardId;
            return View(reloaded);
        }

        ShowSuccess("Връзката с охранител беше запазена.");
        return RedirectToAction(nameof(Index));
    }

    private void ShowResult(OperationResult result, string successMessage)
    {
        if (result.Succeeded)
        {
            ShowSuccess(successMessage);
        }
        else
        {
            ShowError(result.Error!);
        }
    }
}

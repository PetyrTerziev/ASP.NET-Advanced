using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using GuardPost.Common;
using GuardPost.Services.Core.Contracts;
using GuardPost.Web.Infrastructure.Extensions;
using GuardPost.Web.ViewModels.LeaveRequests;

namespace GuardPost.Web.Controllers;

/// <summary>Leave requests of the signed-in guard ("Моите отпуски").</summary>
[Authorize]
public class LeaveRequestsController : BaseController
{
    private readonly ILeaveRequestService leaveRequestService;
    private readonly IGuardService guardService;
    private readonly INotificationService notificationService;

    public LeaveRequestsController(
        ILeaveRequestService leaveRequestService,
        IGuardService guardService,
        INotificationService notificationService)
    {
        this.leaveRequestService = leaveRequestService;
        this.guardService = guardService;
        this.notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        int? guardId = await guardService.GetGuardIdForUserAsync(User.GetUserId());
        if (guardId is null)
        {
            return View("NoGuardProfile");
        }

        var model = await leaveRequestService.GetForGuardAsync(guardId.Value);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        int? guardId = await guardService.GetGuardIdForUserAsync(User.GetUserId());
        if (guardId is null)
        {
            return View("NoGuardProfile");
        }

        return View(new LeaveRequestFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(LeaveRequestFormViewModel model)
    {
        string userId = User.GetUserId();
        int? guardId = await guardService.GetGuardIdForUserAsync(userId);
        if (guardId is null)
        {
            return View("NoGuardProfile");
        }

        if (ModelState.IsValid)
        {
            AddModelErrors(await leaveRequestService.ValidateAsync(model, guardId.Value));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await leaveRequestService.CreateAsync(model, guardId.Value, userId);

        await notificationService.NotifyAdministratorsAsync(
            "Нова заявка за отпуск",
            $"{model.StartDate:dd.MM.yyyy} – {model.EndDate:dd.MM.yyyy}",
            Url.Action("Index", "LeaveRequests", new { area = RoleNames.AdminArea }));

        ShowSuccess("Заявката беше изпратена за одобрение.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        int? guardId = await guardService.GetGuardIdForUserAsync(User.GetUserId());
        if (guardId is null)
        {
            return NotFound();
        }

        var result = await leaveRequestService.CancelAsync(id, guardId.Value);
        if (result.Succeeded)
        {
            ShowSuccess("Заявката беше анулирана.");
        }
        else
        {
            ShowError(result.Error!);
        }

        return RedirectToAction(nameof(Index));
    }
}

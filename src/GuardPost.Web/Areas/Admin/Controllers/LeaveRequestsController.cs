using Microsoft.AspNetCore.Mvc;

using GuardPost.Common;
using GuardPost.Common.Extensions;
using GuardPost.Data.Models.Enums;
using GuardPost.Services.Core.Contracts;
using GuardPost.Web.Infrastructure.Extensions;
using GuardPost.Web.ViewModels.LeaveRequests;

namespace GuardPost.Web.Areas.Admin.Controllers;

public class LeaveRequestsController : AdminBaseController
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
    public async Task<IActionResult> Index(LeaveStatus? status, int? guardId, int page = 1)
    {
        var model = await leaveRequestService.GetAllAsync(status, guardId, page, ApplicationConstants.DefaultPageSize);
        model.Guards = await guardService.GetDropdownAsync(onlyActive: false);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var model = await leaveRequestService.GetForReviewAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Approve([Bind(Prefix = "Input")] LeaveReviewInputModel input)
        => await DecideAsync(input, approve: true);

    [HttpPost]
    public async Task<IActionResult> Reject([Bind(Prefix = "Input")] LeaveReviewInputModel input)
        => await DecideAsync(input, approve: false);

    private async Task<IActionResult> DecideAsync(LeaveReviewInputModel input, bool approve)
    {
        if (!ModelState.IsValid)
        {
            return await ReviewWithErrorsAsync(input);
        }

        var result = approve
            ? await leaveRequestService.ApproveAsync(input.Id, User.GetUserId(), input.ReviewNote)
            : await leaveRequestService.RejectAsync(input.Id, User.GetUserId(), input.ReviewNote);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return await ReviewWithErrorsAsync(input);
        }

        var status = approve ? LeaveStatus.Approved : LeaveStatus.Rejected;
        string? requesterId = await leaveRequestService.GetRequesterIdAsync(input.Id);
        if (requesterId != null)
        {
            await notificationService.NotifyUserAsync(
                requesterId,
                "Заявка за отпуск",
                $"Вашата заявка е: {status.GetDisplayName().ToLowerInvariant()}.",
                Url.Action("Index", "LeaveRequests", new { area = string.Empty }));
        }

        ShowSuccess(approve ? "Отпускът беше одобрен." : "Заявката беше отказана.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReviewWithErrorsAsync(LeaveReviewInputModel input)
    {
        var model = await leaveRequestService.GetForReviewAsync(input.Id);
        if (model is null)
        {
            return NotFound();
        }

        model.Input = input;
        return View(nameof(Review), model);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using GuardPost.Common;
using GuardPost.Services.Core.Contracts;
using GuardPost.Web.Infrastructure.Extensions;
using GuardPost.Web.ViewModels.MySchedule;

namespace GuardPost.Web.Controllers;

/// <summary>Personal pages of a guard: start page and weekly schedule.</summary>
[Authorize]
public class MyScheduleController : BaseController
{
    private const int IncidentsOnDashboard = 5;

    private readonly IGuardService guardService;
    private readonly IShiftService shiftService;
    private readonly ILeaveRequestService leaveRequestService;
    private readonly IIncidentService incidentService;

    public MyScheduleController(
        IGuardService guardService,
        IShiftService shiftService,
        ILeaveRequestService leaveRequestService,
        IIncidentService incidentService)
    {
        this.guardService = guardService;
        this.shiftService = shiftService;
        this.leaveRequestService = leaveRequestService;
        this.incidentService = incidentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        string userId = User.GetUserId();
        int? guardId = await guardService.GetGuardIdForUserAsync(userId);

        var model = new MyDashboardViewModel
        {
            FullName = User.Identity?.Name ?? string.Empty,
            MyIncidents = (await incidentService.GetForUserAsync(userId, 1, IncidentsOnDashboard)).Result.Items,
        };

        if (guardId.HasValue)
        {
            model.Guard = await guardService.GetDetailsAsync(guardId.Value);
            model.FullName = model.Guard?.FullName ?? model.FullName;
            model.LeaveRequests = await leaveRequestService.GetForGuardAsync(guardId.Value);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Week(DateTime? weekStart)
    {
        int? guardId = await guardService.GetGuardIdForUserAsync(User.GetUserId());
        if (guardId is null)
        {
            ShowError(ValidationMessages.NoGuardProfile);
            return RedirectToAction(nameof(Index));
        }

        var model = await shiftService.GetWeekAsync(weekStart, siteId: null, guardId: guardId);
        return View(model);
    }
}

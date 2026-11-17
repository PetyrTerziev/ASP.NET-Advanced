using Microsoft.AspNetCore.Mvc;

using GuardPost.Services.Core.Contracts;

namespace GuardPost.Web.Areas.Admin.Controllers;

public class DashboardController : AdminBaseController
{
    private readonly IDashboardService dashboardService;
    private readonly ILeaveRequestService leaveRequestService;

    public DashboardController(IDashboardService dashboardService, ILeaveRequestService leaveRequestService)
    {
        this.dashboardService = dashboardService;
        this.leaveRequestService = leaveRequestService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = await dashboardService.GetDashboardAsync();
        ViewData["PendingLeaves"] = await leaveRequestService.CountPendingAsync();

        return View(model);
    }
}

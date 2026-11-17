using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using GuardPost.Common;
using GuardPost.Services.Core.Contracts;
using GuardPost.Web.ViewModels.Home;

namespace GuardPost.Web.Controllers.Api;

/// <summary>GET api/dashboard/coverage – refreshed every minute by wwwroot/js/dashboard.js.</summary>
[ApiController]
[Authorize(Roles = RoleNames.Administrator)]
[Route("api/dashboard")]
public class DashboardApiController : ControllerBase
{
    private readonly IDashboardService dashboardService;

    public DashboardApiController(IDashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    [HttpGet("coverage")]
    public async Task<ActionResult<IEnumerable<SiteCoverageViewModel>>> Coverage()
        => Ok(await dashboardService.GetCoverageAsync());
}

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using GuardPost.Common;
using GuardPost.Web.Infrastructure.Extensions;
using GuardPost.Web.ViewModels.Home;

namespace GuardPost.Web.Controllers;

public class HomeController : BaseController
{
    private readonly ILogger<HomeController> logger;

    public HomeController(ILogger<HomeController> logger)
    {
        this.logger = logger;
    }

    /// <summary>Public landing page; signed-in users are sent to their own start page.</summary>
    [HttpGet]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return User.IsAdministrator()
                ? RedirectToAction("Index", "Dashboard", new { area = RoleNames.AdminArea })
                : RedirectToAction("Index", "MySchedule");
        }

        return View();
    }

    [HttpGet]
    public IActionResult About() => View();

    /// <summary>Custom 500 page (see UseExceptionHandler in Program.cs).</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        string requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        logger.LogError("Unhandled error page shown for request {RequestId}.", requestId);

        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View(new ErrorViewModel { RequestId = requestId });
    }

    /// <summary>Custom page for 404 / 400 / 403 and other status codes (see UseStatusCodePagesWithReExecute).</summary>
    [HttpGet]
    public IActionResult HttpError(int code)
    {
        Response.StatusCode = code;
        return View(new ErrorViewModel { StatusCode = code });
    }
}

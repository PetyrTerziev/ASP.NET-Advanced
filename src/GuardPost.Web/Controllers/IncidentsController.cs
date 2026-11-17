using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using GuardPost.Common;
using GuardPost.Common.Providers;
using GuardPost.Services.Core.Contracts;
using GuardPost.Web.Infrastructure.Extensions;
using GuardPost.Web.ViewModels.Incidents;

namespace GuardPost.Web.Controllers;

/// <summary>Incidents reported by the signed-in user ("Моите сигнали").</summary>
[Authorize]
public class IncidentsController : BaseController
{
    private readonly IIncidentService incidentService;
    private readonly ISiteService siteService;
    private readonly IGuardService guardService;
    private readonly INotificationService notificationService;
    private readonly IDateTimeProvider clock;

    public IncidentsController(
        IIncidentService incidentService,
        ISiteService siteService,
        IGuardService guardService,
        INotificationService notificationService,
        IDateTimeProvider clock)
    {
        this.incidentService = incidentService;
        this.siteService = siteService;
        this.guardService = guardService;
        this.notificationService = notificationService;
        this.clock = clock;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var model = await incidentService.GetForUserAsync(User.GetUserId(), page, ApplicationConstants.DefaultPageSize);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        // Users may open only their own reports – another id in the URL gives 404 (parameter tampering).
        if (!await incidentService.CanAccessAsync(id, User.GetUserId(), User.IsAdministrator()))
        {
            return NotFound();
        }

        var model = await incidentService.GetDetailsAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? siteId)
    {
        var now = clock.Now;
        var model = new ReportIncidentViewModel
        {
            SiteId = siteId,
            OccurredOn = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0),
            Sites = await siteService.GetDropdownAsync(onlyActive: true),
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ReportIncidentViewModel model)
    {
        var sites = await siteService.GetDropdownAsync(onlyActive: true);
        if (model.SiteId.HasValue && sites.All(s => s.Id != model.SiteId.Value))
        {
            ModelState.AddModelError(nameof(model.SiteId), ValidationMessages.SiteNotAvailable);
        }

        if (!ModelState.IsValid)
        {
            model.Sites = sites;
            return View(model);
        }

        string userId = User.GetUserId();
        int? guardId = await guardService.GetGuardIdForUserAsync(userId);
        int id = await incidentService.ReportAsync(model, userId, guardId);

        await notificationService.NotifyAdministratorsAsync(
            "Нов сигнал за инцидент",
            model.Title,
            Url.Action("Details", "Incidents", new { area = RoleNames.AdminArea, id }));

        ShowSuccess("Сигналът беше изпратен. Администраторите са уведомени.");
        return RedirectToAction(nameof(Details), new { id });
    }
}

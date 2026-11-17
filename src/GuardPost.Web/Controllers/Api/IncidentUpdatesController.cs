using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using GuardPost.Common;
using GuardPost.Services.Core.Contracts;
using GuardPost.Web.Infrastructure.Extensions;
using GuardPost.Web.ViewModels.Incidents;

namespace GuardPost.Web.Controllers.Api;

/// <summary>
/// REST endpoints for the incident timeline – used by wwwroot/js/incident-timeline.js (AJAX).
/// GET  api/incidents/{incidentId}/updates
/// POST api/incidents/{incidentId}/updates   (JSON body, requires the anti-forgery token header)
/// </summary>
[ApiController]
[Authorize]
[Route("api/incidents/{incidentId:int}/updates")]
public class IncidentUpdatesController : ControllerBase
{
    private readonly IIncidentUpdateService incidentUpdateService;
    private readonly IIncidentService incidentService;
    private readonly INotificationService notificationService;

    public IncidentUpdatesController(
        IIncidentUpdateService incidentUpdateService,
        IIncidentService incidentService,
        INotificationService notificationService)
    {
        this.incidentUpdateService = incidentUpdateService;
        this.incidentService = incidentService;
        this.notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<IncidentUpdateViewModel>>> Get(int incidentId)
    {
        if (!await incidentService.CanAccessAsync(incidentId, User.GetUserId(), User.IsAdministrator()))
        {
            return NotFound();
        }

        return Ok(await incidentUpdateService.GetForIncidentAsync(incidentId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<IncidentUpdateViewModel>> Post(int incidentId, [FromBody] IncidentUpdateInputModel input)
    {
        if (!await incidentService.CanAccessAsync(incidentId, User.GetUserId(), User.IsAdministrator()))
        {
            return NotFound();
        }

        // [ApiController] already returns 400 with the validation errors when the model is invalid.
        var update = await incidentUpdateService.AddAsync(incidentId, User.GetUserId(), input.Text);

        if (!User.IsAdministrator())
        {
            await notificationService.NotifyAdministratorsAsync(
                "Ново действие по инцидент",
                update.Text,
                Url.Action("Details", "Incidents", new { area = RoleNames.AdminArea, id = incidentId }));
        }

        return Ok(update);
    }
}

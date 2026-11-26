using GuardPost.Web.ViewModels.Incidents;

namespace GuardPost.Services.Core.Contracts;

public interface IIncidentUpdateService
{
    Task<IReadOnlyList<IncidentUpdateViewModel>> GetForIncidentAsync(int incidentId);

    Task<IncidentUpdateViewModel> AddAsync(int incidentId, string authorId, string text);
}

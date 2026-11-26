using GuardPost.Data.Models.Enums;
using GuardPost.Web.ViewModels.Common;
using GuardPost.Web.ViewModels.Incidents;

namespace GuardPost.Services.Core.Contracts;

public interface IIncidentService
{
    Task<IncidentIndexViewModel> GetIndexAsync(
        string? searchTerm,
        IncidentSeverity? severity,
        IncidentStatus? status,
        int? siteId,
        int page,
        int pageSize);

    Task<IncidentDetailsViewModel?> GetDetailsAsync(int id);

    Task<IncidentFormViewModel?> GetForEditAsync(int id);

    Task<IReadOnlyList<ValidationError>> ValidateAsync(IncidentFormViewModel model);

    Task<int> CreateAsync(IncidentFormViewModel model, string? createdById = null);

    /// <summary>Incident reported by a guard from the user part of the site (always with status "Open").</summary>
    Task<int> ReportAsync(ReportIncidentViewModel model, string userId, int? guardId);

    Task<MyIncidentsViewModel> GetForUserAsync(string userId, int page, int pageSize);

    /// <summary>Administrators can open every incident, users only the ones they registered.</summary>
    Task<bool> CanAccessAsync(int incidentId, string userId, bool isAdministrator);

    Task<bool> UpdateAsync(IncidentFormViewModel model);

    Task<bool> DeleteAsync(int id);
}

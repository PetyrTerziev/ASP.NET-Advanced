using System.Linq.Expressions;

using GuardPost.Data.Models;
using GuardPost.Web.ViewModels.Incidents;
using GuardPost.Web.ViewModels.LeaveRequests;
using GuardPost.Web.ViewModels.Shifts;

namespace GuardPost.Services.Core.Mapping;

/// <summary>
/// Reusable LINQ projections – translated to SQL by EF Core, so only the needed columns are loaded.
/// </summary>
public static class Projections
{
    public static readonly Expression<Func<Shift, ShiftListItemViewModel>> ShiftToListItem = s => new ShiftListItemViewModel
    {
        Id = s.Id,
        SiteId = s.SiteId,
        SiteName = s.Site.Name,
        GuardId = s.GuardId,
        GuardName = s.Guard.FirstName + " " + s.Guard.LastName,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        Notes = s.Notes,
    };

    public static readonly Expression<Func<Incident, IncidentListItemViewModel>> IncidentToListItem = i => new IncidentListItemViewModel
    {
        Id = i.Id,
        Title = i.Title,
        SiteId = i.SiteId,
        SiteName = i.Site.Name,
        OccurredOn = i.OccurredOn,
        Severity = i.Severity,
        Status = i.Status,
        ReportedBy = i.ReportedByGuard == null
            ? null
            : i.ReportedByGuard.FirstName + " " + i.ReportedByGuard.LastName,
    };

    public static readonly Expression<Func<LeaveRequest, LeaveRequestListItemViewModel>> LeaveToListItem = l => new LeaveRequestListItemViewModel
    {
        Id = l.Id,
        GuardId = l.GuardId,
        GuardName = l.Guard.FirstName + " " + l.Guard.LastName,
        StartDate = l.StartDate,
        EndDate = l.EndDate,
        Type = l.Type,
        Status = l.Status,
        Reason = l.Reason,
        CreatedOn = l.CreatedOn,
        ReviewedBy = l.ReviewedBy == null ? null : l.ReviewedBy.FirstName + " " + l.ReviewedBy.LastName,
        ReviewedOn = l.ReviewedOn,
        ReviewNote = l.ReviewNote,
    };
}

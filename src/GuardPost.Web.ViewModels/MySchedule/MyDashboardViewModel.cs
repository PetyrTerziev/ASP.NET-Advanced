using GuardPost.Web.ViewModels.Guards;
using GuardPost.Web.ViewModels.Incidents;
using GuardPost.Web.ViewModels.LeaveRequests;

namespace GuardPost.Web.ViewModels.MySchedule;

/// <summary>Personal start page of a guard.</summary>
public class MyDashboardViewModel
{
    public string FullName { get; set; } = null!;

    /// <summary>Null when the account is not linked to a guard yet.</summary>
    public GuardDetailsViewModel? Guard { get; set; }

    public IEnumerable<LeaveRequestListItemViewModel> LeaveRequests { get; set; } = new List<LeaveRequestListItemViewModel>();

    public IEnumerable<IncidentListItemViewModel> MyIncidents { get; set; } = new List<IncidentListItemViewModel>();
}

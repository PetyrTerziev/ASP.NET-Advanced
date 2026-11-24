using GuardPost.Web.ViewModels.Common;

namespace GuardPost.Web.ViewModels.Incidents;

public class MyIncidentsViewModel
{
    public PagedResult<IncidentListItemViewModel> Result { get; set; } = null!;
}

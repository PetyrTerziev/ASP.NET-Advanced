using GuardPost.Data.Models.Enums;
using GuardPost.Web.ViewModels.Common;

namespace GuardPost.Web.ViewModels.LeaveRequests;

public class LeaveRequestIndexViewModel
{
    public LeaveStatus? Status { get; set; }

    public int? GuardId { get; set; }

    public int PendingCount { get; set; }

    public IEnumerable<DropdownItemViewModel> Guards { get; set; } = new List<DropdownItemViewModel>();

    public PagedResult<LeaveRequestListItemViewModel> Result { get; set; } = null!;
}

using GuardPost.Data.Models.Enums;

namespace GuardPost.Web.ViewModels.LeaveRequests;

public class LeaveRequestListItemViewModel
{
    public int Id { get; set; }

    public int GuardId { get; set; }

    public string GuardName { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int Days => (EndDate.Date - StartDate.Date).Days + 1;

    public LeaveType Type { get; set; }

    public LeaveStatus Status { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedOn { get; set; }

    public string? ReviewNote { get; set; }
}

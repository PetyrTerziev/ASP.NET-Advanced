using GuardPost.Data.Models.Enums;

namespace GuardPost.Data.Models;

/// <summary>
/// A request for leave (annual, sick, unpaid). While a request is approved
/// the guard cannot be scheduled for shifts in that period.
/// </summary>
public class LeaveRequest
{
    public int Id { get; set; }

    public int GuardId { get; set; }

    public virtual Guard Guard { get; set; } = null!;

    /// <summary>First day of the leave (inclusive).</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Last day of the leave (inclusive).</summary>
    public DateTime EndDate { get; set; }

    public LeaveType Type { get; set; }

    public string? Reason { get; set; }

    public LeaveStatus Status { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? RequestedById { get; set; }

    public virtual ApplicationUser? RequestedBy { get; set; }

    public string? ReviewedById { get; set; }

    public virtual ApplicationUser? ReviewedBy { get; set; }

    public DateTime? ReviewedOn { get; set; }

    public string? ReviewNote { get; set; }
}

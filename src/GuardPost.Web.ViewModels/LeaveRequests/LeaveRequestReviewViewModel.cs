using System.ComponentModel.DataAnnotations;

using GuardPost.Common;
using GuardPost.Web.ViewModels.Shifts;

using static GuardPost.Common.EntityValidationConstants.LeaveRequest;

namespace GuardPost.Web.ViewModels.LeaveRequests;

public class LeaveRequestReviewViewModel
{
    public LeaveRequestListItemViewModel Request { get; set; } = null!;

    /// <summary>Shifts of the guard inside the requested period – they block approval.</summary>
    public IEnumerable<ShiftListItemViewModel> ConflictingShifts { get; set; } = new List<ShiftListItemViewModel>();

    public LeaveReviewInputModel Input { get; set; } = new();
}

public class LeaveReviewInputModel
{
    public int Id { get; set; }

    [StringLength(ReviewNoteMaxLength, ErrorMessage = ValidationMessages.MaxLength)]
    [Display(Name = "Бележка към решението")]
    public string? ReviewNote { get; set; }
}

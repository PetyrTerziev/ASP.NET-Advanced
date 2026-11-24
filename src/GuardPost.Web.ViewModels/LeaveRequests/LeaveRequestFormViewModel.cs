using System.ComponentModel.DataAnnotations;

using GuardPost.Common;
using GuardPost.Data.Models.Enums;
using GuardPost.Web.ViewModels.Validation;

using static GuardPost.Common.EntityValidationConstants.LeaveRequest;

namespace GuardPost.Web.ViewModels.LeaveRequests;

public class LeaveRequestFormViewModel
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [DataType(DataType.Date)]
    [Display(Name = "Първи ден")]
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = ValidationMessages.Required)]
    [DataType(DataType.Date)]
    [DateAfter(nameof(StartDate), AllowEqual = true, ErrorMessage = ValidationMessages.LeaveEndBeforeStart)]
    [Display(Name = "Последен ден")]
    public DateTime? EndDate { get; set; }

    [Required(ErrorMessage = ValidationMessages.Required)]
    [EnumDataType(typeof(LeaveType))]
    [Display(Name = "Вид отпуск")]
    public LeaveType? Type { get; set; } = LeaveType.Paid;

    [StringLength(ReasonMaxLength, ErrorMessage = ValidationMessages.MaxLength)]
    [Display(Name = "Причина / бележка")]
    public string? Reason { get; set; }
}

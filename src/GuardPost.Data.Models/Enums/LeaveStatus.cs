using System.ComponentModel.DataAnnotations;

namespace GuardPost.Data.Models.Enums;

public enum LeaveStatus
{
    [Display(Name = "Чака одобрение")]
    Pending = 0,

    [Display(Name = "Одобрен")]
    Approved = 1,

    [Display(Name = "Отказан")]
    Rejected = 2,

    [Display(Name = "Анулиран")]
    Cancelled = 3,
}

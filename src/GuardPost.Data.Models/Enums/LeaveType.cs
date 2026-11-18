using System.ComponentModel.DataAnnotations;

namespace GuardPost.Data.Models.Enums;

public enum LeaveType
{
    [Display(Name = "Платен годишен отпуск")]
    Paid = 0,

    [Display(Name = "Болничен")]
    Sick = 1,

    [Display(Name = "Неплатен отпуск")]
    Unpaid = 2,

    [Display(Name = "Друг")]
    Other = 3,
}

using System.ComponentModel.DataAnnotations;

using GuardPost.Common;

namespace GuardPost.Web.ViewModels.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EmailAddress(ErrorMessage = ValidationMessages.InvalidEmail)]
    [Display(Name = "Имейл")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [DataType(DataType.Password)]
    [Display(Name = "Парола")]
    public string Password { get; set; } = null!;

    [Display(Name = "Запомни ме")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

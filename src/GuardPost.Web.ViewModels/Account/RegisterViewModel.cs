using System.ComponentModel.DataAnnotations;

using GuardPost.Common;

using static GuardPost.Common.EntityValidationConstants.User;

namespace GuardPost.Web.ViewModels.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FirstNameMaxLength, MinimumLength = FirstNameMinLength, ErrorMessage = ValidationMessages.StringLength)]
    [Display(Name = "Име")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(LastNameMaxLength, MinimumLength = LastNameMinLength, ErrorMessage = ValidationMessages.StringLength)]
    [Display(Name = "Фамилия")]
    public string LastName { get; set; } = null!;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [EmailAddress(ErrorMessage = ValidationMessages.InvalidEmail)]
    [StringLength(EmailMaxLength, ErrorMessage = ValidationMessages.MaxLength)]
    [Display(Name = "Имейл")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(PasswordMaxLength, MinimumLength = PasswordMinLength, ErrorMessage = ValidationMessages.StringLength)]
    [DataType(DataType.Password)]
    [Display(Name = "Парола")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = ValidationMessages.PasswordsDoNotMatch)]
    [Display(Name = "Повторете паролата")]
    public string ConfirmPassword { get; set; } = null!;
}

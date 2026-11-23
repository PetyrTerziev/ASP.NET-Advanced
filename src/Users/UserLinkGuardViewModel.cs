using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

using GuardPost.Web.ViewModels.Common;

namespace GuardPost.Web.ViewModels.Users;

public class UserLinkGuardViewModel
{
    public string UserId { get; set; } = null!;

    [BindNever]
    [ValidateNever]
    public string FullName { get; set; } = string.Empty;

    [BindNever]
    [ValidateNever]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Охранител")]
    public int? GuardId { get; set; }

    [BindNever]
    [ValidateNever]
    public IEnumerable<DropdownItemViewModel> Guards { get; set; } = new List<DropdownItemViewModel>();
}

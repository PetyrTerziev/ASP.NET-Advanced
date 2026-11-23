using GuardPost.Web.ViewModels.Common;

namespace GuardPost.Web.ViewModels.Users;

public class UserIndexViewModel
{
    public string? SearchTerm { get; set; }

    public PagedResult<UserListItemViewModel> Result { get; set; } = null!;
}

namespace GuardPost.Web.ViewModels.Users;

public class UserListItemViewModel
{
    public string Id { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public bool IsAdministrator { get; set; }

    public bool IsLockedOut { get; set; }

    public int? GuardId { get; set; }

    public string? GuardName { get; set; }

    public DateTime CreatedOn { get; set; }
}

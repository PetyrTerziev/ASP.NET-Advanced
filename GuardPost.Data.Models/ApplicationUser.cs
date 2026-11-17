using Microsoft.AspNetCore.Identity;

namespace GuardPost.Data.Models;

/// <summary>
/// An account in the system. A user with the "User" role is normally a guard –
/// the link to the guard record gives access to "My schedule", "My reports" and "My leave".
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public int? GuardId { get; set; }

    public virtual Guard? Guard { get; set; }
}

using System.Security.Claims;

using GuardPost.Common;

namespace GuardPost.Web.Infrastructure.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? throw new InvalidOperationException("The current user is not authenticated.");

    public static bool IsAdministrator(this ClaimsPrincipal user)
        => user.IsInRole(RoleNames.Administrator);
}

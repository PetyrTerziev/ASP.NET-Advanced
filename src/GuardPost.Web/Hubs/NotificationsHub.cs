using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using GuardPost.Common;

namespace GuardPost.Web.Hubs;

/// <summary>
/// Real-time notifications (SignalR). Administrators join a common group;
/// every user can also be reached individually through Clients.User(userId).
/// </summary>
[Authorize]
public class NotificationsHub : Hub
{
    public const string Path = "/hubs/notifications";
    public const string AdministratorsGroup = "administrators";
    public const string NotifyMethod = "notify";

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole(RoleNames.Administrator) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdministratorsGroup);
        }

        await base.OnConnectedAsync();
    }
}

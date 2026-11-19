using Microsoft.AspNetCore.SignalR;

using GuardPost.Services.Core.Contracts;
using GuardPost.Web.Hubs;

namespace GuardPost.Web.Infrastructure.Notifications;

/// <summary>SignalR implementation of <see cref="INotificationService"/>.</summary>
public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationsHub> hubContext;
    private readonly ILogger<SignalRNotificationService> logger;

    public SignalRNotificationService(IHubContext<NotificationsHub> hubContext, ILogger<SignalRNotificationService> logger)
    {
        this.hubContext = hubContext;
        this.logger = logger;
    }

    public Task NotifyAdministratorsAsync(string title, string message, string? url = null)
        => SendAsync(hubContext.Clients.Group(NotificationsHub.AdministratorsGroup), title, message, url);

    public Task NotifyUserAsync(string userId, string title, string message, string? url = null)
        => SendAsync(hubContext.Clients.User(userId), title, message, url);

    private async Task SendAsync(IClientProxy clients, string title, string message, string? url)
    {
        try
        {
            await clients.SendAsync(NotificationsHub.NotifyMethod, new { title, message, url });
        }
        catch (Exception ex)
        {
            // A notification must never break the main operation (the data is already saved).
            logger.LogWarning(ex, "Real-time notification could not be sent.");
        }
    }
}

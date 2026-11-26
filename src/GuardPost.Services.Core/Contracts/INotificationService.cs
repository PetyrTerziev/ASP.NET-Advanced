namespace GuardPost.Services.Core.Contracts;

/// <summary>
/// Sends real-time notifications. The web project implements it with SignalR;
/// tests and background jobs can use any other implementation.
/// </summary>
public interface INotificationService
{
    Task NotifyAdministratorsAsync(string title, string message, string? url = null);

    Task NotifyUserAsync(string userId, string title, string message, string? url = null);
}

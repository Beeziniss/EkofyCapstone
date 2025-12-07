using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Notifications;

public interface INotificationService
{
    IQueryable<Notification> GetNotifications();
    IQueryable<Notification> GetNotificationsForUser(string userId);
    Task<bool> MarkNotificationAsReadAsync(string notificationId);

    //Task<bool> SendFcmToken(string userId, string token);

    Task SendFcmNotificationAsync(string? userId, string title, string body, string channelId, Dictionary<string, string>? data = null);
}

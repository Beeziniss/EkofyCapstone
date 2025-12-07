using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Notifications;

public interface INotificationService
{
    IQueryable<Notification> GetNotifications();
    IQueryable<Notification> GetNotificationsForUser(string userId);
    Task SendFcmNotificationAsync(string fcmToken, string title, string body, string channelId, Dictionary<string, string>? data = null);
    Task<bool> SendFcmToken(string userId, string token);
    Task SendMultipleMessageAsync(IReadOnlyList<string> fcmTokens, string title, string body, string channelId, Dictionary<string, string>? data = null);
}

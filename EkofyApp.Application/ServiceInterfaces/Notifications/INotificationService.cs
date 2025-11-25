using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Notifications;

public interface INotificationService
{
    IQueryable<Notification> GetNotifications();
    IQueryable<Notification> GetNotificationsForUser(string userId);
}

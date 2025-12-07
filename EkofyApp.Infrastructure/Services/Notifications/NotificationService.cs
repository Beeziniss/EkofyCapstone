using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Notifications;
using EkofyApp.Domain.Entities;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using Serilog;

namespace EkofyApp.Infrastructure.Services.Notifications;

public sealed class NotificationService(IUnitOfWork unitOfWork, IHubContext<NotificationHub> hubContext) : INotificationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;

    public IQueryable<Domain.Entities.Notification> GetNotifications()
    {
        return _unitOfWork.GetCollection<Domain.Entities.Notification>().AsQueryable();
    }

    public IQueryable<Domain.Entities.Notification> GetNotificationsForUser(string userId)
    {
        return _unitOfWork.GetCollection<Domain.Entities.Notification>().AsQueryable().Where(n => n.TargetId == userId);
    }

    //public async Task<bool> SendFcmToken(string userId, string token)
    //{
    //    UpdateDefinition<User> update = Builders<User>.Update.AddToSet(u => u.FCMToken, token);
    //    var result = await _unitOfWork.GetCollection<User>().UpdateOneAsync(u => u.Id == userId, update);
    //    return result.ModifiedCount > 0;
    //} 

    public async Task SendFcmNotificationAsync(string? userId, string title, string body, string channelId, Dictionary<string, string>? data = null)
    {
        // nếu userId null thì gửi cho tất cả
        string topic = "all_users";

        if (!string.IsNullOrEmpty(userId))
        {
            topic = "user_" + userId;
        }

        // đóng gói message với title + body
        var message = new FirebaseAdmin.Messaging.Message()
        {
            Topic = topic,
            Notification = new FirebaseAdmin.Messaging.Notification
            {
                Title = title,
                Body = body
            },
            Android = new AndroidConfig
            {
                Notification = new AndroidNotification
                {
                    ChannelId = channelId,
                    Priority = NotificationPriority.HIGH
                }
            },
            Data = data ?? []
        };

        await FirebaseMessaging.DefaultInstance.SendAsync(message);
    }
}

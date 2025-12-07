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

    public async Task<bool> SendFcmToken(string userId, string token)
    {
        UpdateDefinition<User> update = Builders<User>.Update.AddToSet(u => u.FCMToken, token);
        var result = await _unitOfWork.GetCollection<User>().UpdateOneAsync(u => u.Id == userId, update);
        return result.ModifiedCount > 0;
    } 

    public async Task SendFcmNotificationAsync(string fcmToken, string title, string body, string channelId, Dictionary<string, string>? data = null)
    {
        var message = new FirebaseAdmin.Messaging.Message()
        {
            Token = fcmToken,
            Notification = new FirebaseAdmin.Messaging.Notification
            {
                Title = title,
                Body = body, 
                ImageUrl = "https://res.cloudinary.com/dofnn7sbx/image/upload/v1764994045/Ekofy_Logo_-_White_xga7t2.png"
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

        string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
        Log.Information($"Successfully sent message: {response}");
    }

    public async Task SendMultipleMessageAsync(IReadOnlyList<string> fcmTokens, string title, string body, string channelId, Dictionary<string, string>? data = null)
    {
        var messages = new MulticastMessage
        {
            Tokens = fcmTokens,
            Notification = new FirebaseAdmin.Messaging.Notification
            {
                Title = title,
                Body = body,
                ImageUrl = "https://res.cloudinary.com/dofnn7sbx/image/upload/v1764994045/Ekofy_Logo_-_White_xga7t2.png"
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
        var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(messages);
        Log.Information($"Successfully sent {response.SuccessCount} messages out of devices");
    }

}

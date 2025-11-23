using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Notifications;
using EkofyApp.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Notifications;

public sealed class NotificationService(IUnitOfWork unitOfWork, IHubContext<NotificationHub> hubContext) : INotificationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;

    public IQueryable<Notification> GetNotificationsForUser(string userId)
    {
        return _unitOfWork.GetCollection<Notification>().AsQueryable().Where(n => n.TargetId == userId);
    }
}

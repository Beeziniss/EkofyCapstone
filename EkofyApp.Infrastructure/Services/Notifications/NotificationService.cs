using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Notifications;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Utils;
using EkofyApp.Infrastructure.Services.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace EkofyApp.Infrastructure.Services.Notifications;

public sealed class NotificationService(IUnitOfWork unitOfWork, IHubContext<NotificationHub> hubContext) : INotificationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;
}

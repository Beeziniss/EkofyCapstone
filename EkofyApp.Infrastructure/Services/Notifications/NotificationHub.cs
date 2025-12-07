using EkofyApp.Application.Models.Notifications;
using EkofyApp.Application.ServiceInterfaces.Notifications;
using EkofyApp.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using System.Collections.Concurrent;

namespace EkofyApp.Infrastructure.Services.Notifications;

public class NotificationHub(INotificationService notificationService) : Hub
{
    // userId → list of connectionIds (hỗ trợ nhiều tab/device)
    private static readonly ConcurrentDictionary<string, HashSet<string>> _connections = new();
    private readonly INotificationService _notificationService = notificationService;

    public override async Task OnConnectedAsync()
    {
        string? userId = Context.User?.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("ReceiveException", "Session expired. Please login again.");
            Context.Abort();
            return;
        }

        var userConnections = _connections.GetOrAdd(userId, _ => []);
        lock (userConnections)
        {
            userConnections.Add(Context.ConnectionId);
        }

        Console.WriteLine($"✅ {userId} connected to NotificationHub ({Context.ConnectionId})");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? userId = Context.User?.FindFirst("userId")?.Value;

        if (!string.IsNullOrEmpty(userId) && _connections.TryGetValue(userId, out var userConnections))
        {
            lock (userConnections)
            {
                userConnections.Remove(Context.ConnectionId);
                if (userConnections.Count == 0)
                    _connections.TryRemove(userId, out _);
            }

            Console.WriteLine($"❌ {userId} disconnected from NotificationHub ({Context.ConnectionId})");
        }

        await base.OnDisconnectedAsync(exception);
    }

    //public async Task SendNotificationToUsers(IEnumerable<string> userIds, NotificationResponse notificationResponse)
    //{
    //    List<string> allConnections = [];

    //    foreach (string? userId in userIds.Distinct())
    //    {
    //        if (_connections.TryGetValue(userId, out HashSet<string>? userConnections))
    //        {
    //            lock (userConnections)
    //            {
    //                allConnections.AddRange(userConnections);
    //            }
    //        }
    //    }

    //    if (allConnections.Count != 0)
    //    {
    //        await Clients.Clients(allConnections).SendAsync("ReceiveNotification", notificationResponse);
    //    }
    //}

    //// Optional: for debugging
    //public static IReadOnlyCollection<string> GetUserConnectionIds(string userId)
    //{
    //    return _connections.TryGetValue(userId, out var set) ? set.ToList() : [];
    //}


    public async Task MarkNotificationAsRead(string notificationId)
    {
        string? userId = Context.User?.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("ReceiveException", "Session expired. Please login again.");
            return;
        }

        // Gọi phương thức cập nhật trạng thái thông báo trong cơ sở dữ liệu
        var success = await _notificationService.MarkNotificationAsReadAsync(notificationId);

        if (success)
        {
            // Nếu thành công, có thể gửi thông báo lại cho các client khác nếu cần
            await Clients.User(userId).SendAsync("NotificationRead", notificationId);
        }
        else
        {
            await Clients.Caller.SendAsync("ReceiveException", "Failed to mark notification as read.");
        }
    }

}

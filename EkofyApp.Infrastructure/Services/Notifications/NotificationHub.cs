using EkofyApp.Application.Models.Notifications;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace EkofyApp.Infrastructure.Services.Notifications;

public class NotificationHub : Hub
{
    // userId → list of connectionIds (hỗ trợ nhiều tab/device)
    private static readonly ConcurrentDictionary<string, HashSet<string>> _connections = new();

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
}

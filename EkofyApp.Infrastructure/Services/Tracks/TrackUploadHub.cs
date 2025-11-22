using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace EkofyApp.Infrastructure.Services.Tracks;

public sealed class TrackUploadHub : Hub
{
    private static readonly ConcurrentDictionary<string, HashSet<string>> OnlineUsers = []; // readerId -> senderConnectionId

    public override async Task OnConnectedAsync()
    {
        string? userId = Context.User?.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            // Gửi lỗi rồi disconnect
            await Clients.Caller.SendAsync("ReceiveException", "Your session has expired. Please login again.");
            Context.Abort(); // Ngắt kết nối client
            return;
        }

        HashSet<string> connections = OnlineUsers.GetOrAdd(userId, _ => []);

        lock (connections)
        {
            connections.Add(Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    // Ngắt kết nối
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? userId = Context.User?.FindFirst("userId")?.Value;

        if (!string.IsNullOrEmpty(userId) && OnlineUsers.TryGetValue(userId, out HashSet<string>? connections))
        {
            lock (connections)
            {
                connections.Remove(Context.ConnectionId);

                if (connections.Count == 0)
                {
                    OnlineUsers.TryRemove(userId, out _);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}

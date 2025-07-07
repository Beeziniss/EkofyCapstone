using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HealthyNutritionApp.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace EkofyApp.Infrastructure.Services.Chat;
public class ChatHub(IUnitOfWork unitOfWork) : Hub
{
    private static readonly ConcurrentDictionary<string, string> OnlineUsers = []; // userId -> senderConnectionId
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public override async Task OnConnectedAsync()
    {
        string? userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            // Gửi lỗi rồi disconnect
            await Clients.Caller.SendAsync("ReceiveException", "Your session has expired. Please login again.");
            Context.Abort(); // Ngắt kết nối client
            return;
        }

        // Ghi đè kết nối cũ nếu đã tồn tại (tránh trùng user)
        OnlineUsers.AddOrUpdate(userId, Context.ConnectionId, (key, oldValue) => Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    // Ngắt kết nối
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        string? userId = OnlineUsers
            .FirstOrDefault(x => x.Value == Context.ConnectionId).Key;

        if (!string.IsNullOrEmpty(userId))
        {
            OnlineUsers.TryRemove(userId, out _);
        }

        return base.OnDisconnectedAsync(exception);
    }

    // SEND MESSAGE
    public async Task SendMessage(string conversationId, string senderId, string receiverId, string text)
    {
        Message message = new()
        {
            ConversationId = conversationId,
            SenderId = senderId,
            ReceiverId = receiverId,
            Text = text,
            SentAt = TimeControl.GetUtcPlus7Time(),
        };

        await _unitOfWork.GetCollection<Message>().InsertOneAsync(message);

        // Update lastMessage
        UpdateDefinition<Conversation> update = Builders<Conversation>.Update
            .Set(c => c.LastMessage, new LastMessage
            {
                Text = text,
                SenderId = senderId,
                SentAt = message.SentAt,
                IsReadBy = [senderId]
            })
            .Set(c => c.UpdatedAt, TimeControl.GetUtcPlus7Time());

        await _unitOfWork.GetCollection<Conversation>().UpdateOneAsync(
            Builders<Conversation>.Filter.Eq(c => c.Id, conversationId),
            update);

        // SignalR push to receiver
        if (OnlineUsers.TryGetValue(receiverId, out string? receiverConnId))
        {
            await Clients.Client(receiverConnId).SendAsync("ReceiveMessage", message);
        }

        // Optional: return ack
        await Clients.Caller.SendAsync("MessageSent", message);
    }

    // MARK AS READ
    public async Task MarkAsRead(string conversationId, string userId)
    {
        // Mark all as read
        FilterDefinition<Message> filter = Builders<Message>.Filter.And(
            Builders<Message>.Filter.Eq(x => x.ConversationId, conversationId),
            Builders<Message>.Filter.Eq(x => x.ReceiverId, userId),
            Builders<Message>.Filter.Eq(x => x.IsRead, false)
        );

        await _unitOfWork.GetCollection<Message>().UpdateManyAsync(filter, Builders<Message>.Update.Set(x => x.IsRead, true));

        // Update lastMessage.isReadBy
        await _unitOfWork.GetCollection<Conversation>().UpdateOneAsync(
            Builders<Conversation>.Filter.Eq(x => x.Id, conversationId),
            Builders<Conversation>.Update.AddToSet("lastMessage.isReadBy", userId)
        );

        // Push seen to other client
        Conversation otherUser = await _unitOfWork.GetCollection<Conversation>().Find(x => x.Id == conversationId).FirstOrDefaultAsync();
        string? receiverId = otherUser.UserIds.FirstOrDefault(u => u != userId);

        if (receiverId != null && OnlineUsers.TryGetValue(receiverId, out string? connectionId))
        {
            await Clients.Client(connectionId).SendAsync("MessageSeen", new
            {
                ConversationId = conversationId,
                SeenBy = userId
            });
        }
    }

    public async Task DeleteMessage(string messageId, string userId)
    {
        UpdateResult result = await _unitOfWork.GetCollection<Message>().UpdateOneAsync(Builders<Message>.Filter.Eq(m => m.Id, messageId), Builders<Message>.Update.AddToSet(m => m.DeletedFor, userId));

        if (result.ModifiedCount <= 0)
        {
            // Không tìm thấy message hoặc không có quyền xóa
            await Clients.Caller.SendAsync("ReceiveException", "Message not found or you don't have permission to delete this message.");
            return;
        }
        // Gửi cho cả người gửi + người nhận để cập nhật UI
        Message msg = await _unitOfWork.GetCollection<Message>().Find(Builders<Message>.Filter.Eq(m => m.Id, messageId)).FirstOrDefaultAsync();
        if (msg == null)
        {
            return;
        }

        List<string> connectionIds = [];

        if (OnlineUsers.TryGetValue(msg.SenderId, out string? senderConnectionId))
        {
            connectionIds.Add(senderConnectionId);
        }

        if (OnlineUsers.TryGetValue(msg.ReceiverId, out string? receiverConnectionId))
        {
            connectionIds.Add(receiverConnectionId);
        }

        await Clients.Clients(connectionIds).SendAsync("MessageDeleted", new
        {
            messageId,
            deletedBy = userId
        });
    }
}

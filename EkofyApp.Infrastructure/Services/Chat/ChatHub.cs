using EkofyApp.Application.Models.Chat;
using EkofyApp.Application.Models.Notifications;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using EkofyApp.Infrastructure.Services.Notifications;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace EkofyApp.Infrastructure.Services.Chat;

public sealed class ChatHub(IUnitOfWork unitOfWork, IHubContext<NotificationHub> notificationHubContext) : Hub
{
    private static readonly ConcurrentDictionary<string, HashSet<string>> OnlineUsers = []; // userId -> userConnectionId
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHubContext<NotificationHub> _notificationHubContext = notificationHubContext;

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

    // SEND MESSAGE
    public async Task SendMessage(ChatMessageRequest chatMessageRequest)
    {
        try
        {
            // Nếu chưa có conversationId, tìm hoặc tạo
            if (string.IsNullOrEmpty(chatMessageRequest.ConversationId))
            {
                Conversation conversation = await _unitOfWork.GetCollection<Conversation>()
                    .Find(c => c.UserIds.Contains(chatMessageRequest.SenderId) && c.UserIds.Contains(chatMessageRequest.ReceiverId))
                    .FirstOrDefaultAsync();

                if (conversation is null)
                {
                    conversation = new()
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        UserIds = [chatMessageRequest.SenderId, chatMessageRequest.ReceiverId],
                    };
                    await _unitOfWork.GetCollection<Conversation>().InsertOneAsync(conversation);
                }

                chatMessageRequest.ConversationId = conversation.Id;
            }

            // Atomically check status & update lastMessage
            DateTimeOffset now = HelperMethod.GetUtcPlus7TimeOffset();

            FilterDefinition<Conversation> filter = Builders<Conversation>.Filter.And(
                Builders<Conversation>.Filter.Eq(c => c.Id, chatMessageRequest.ConversationId),
                Builders<Conversation>.Filter.Ne(c => c.Status, ConversationStatus.Completed),
                Builders<Conversation>.Filter.Ne(c => c.Status, ConversationStatus.Cancelled)
            );

            UpdateDefinition<Conversation> update = Builders<Conversation>.Update
                .Set(c => c.LastMessage, new LastMessage
                {
                    Text = chatMessageRequest.Text,
                    SenderId = chatMessageRequest.SenderId,
                    SentAt = now,
                    IsReadBy = [chatMessageRequest.SenderId]
                })
                .Set(c => c.UpdatedAt, now);

            UpdateResult updateResult = await _unitOfWork.GetCollection<Conversation>()
                .UpdateOneAsync(
                    filter,
                    update
                );
            if (updateResult.ModifiedCount == 0)
            {
                // Không thể update, nghĩa là conversation đã đóng
                await Clients.Caller.SendAsync("ReceiveException", "This conversation is closed. You cannot send messages.");
                return;
            }

            // Insert message
            Message message = new()
            {
                ConversationId = chatMessageRequest.ConversationId,
                SenderId = chatMessageRequest.SenderId,
                ReceiverId = chatMessageRequest.ReceiverId,
                Text = chatMessageRequest.Text,
                SentAt = now
            };

            await _unitOfWork.GetCollection<Message>().InsertOneAsync(message);

            // Gửi tới tất cả connection của người nhận
            if (OnlineUsers.TryGetValue(chatMessageRequest.ReceiverId, out HashSet<string>? receiverConnections))
            {
                await Clients.Clients(receiverConnections.ToList()).SendAsync("ReceiveMessage", message);
            }
            else
            {
                dynamic user;
                string role = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
                if (role == UserRole.Listener.ToString())
                {
                    user = await _unitOfWork.GetCollection<Listener>()
                        .Find(l => l.UserId == chatMessageRequest.SenderId)
                        .Project(l => new
                        {
                            Name = l.DisplayName,
                            l.AvatarImage
                        })
                        .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found user {chatMessageRequest.SenderId}");
                }
                else
                {
                    user = await _unitOfWork.GetCollection<Artist>()
                        .Find(a => a.UserId == chatMessageRequest.SenderId)
                        .Project(a => new
                        {
                            Name = a.StageName,
                            a.AvatarImage
                        })
                        .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found user {chatMessageRequest.SenderId}");
                }

                // Người nhận không online, xử lý thông báo push ở đây nếu cần
                await _notificationHubContext.Clients.User(chatMessageRequest.ReceiverId).SendAsync("ReceiveNotification", new NotificationResponse
                {
                    Content = $"You have a new message from {user.Name}.",
                    Avatar = user.Avatar
                });
            }

            // Optional: cũng có thể gửi về cho tất cả kết nối của sender nếu muốn sync
            if (OnlineUsers.TryGetValue(chatMessageRequest.SenderId, out HashSet<string>? senderConnections))
            {
                await Clients.Clients(senderConnections.ToList()).SendAsync("MessageSent", message);
            }

            // Optional: return ack
            //await Clients.Caller.SendAsync("MessageSent", message);
        }
        catch (Exception ex)
        {
            // Gửi lỗi về client
            await Clients.Caller.SendAsync("ReceiveException", $"Error sending message: {ex.Message}");
        }
    }

    // MARK AS READ
    public async Task MarkAsRead(string conversationId, string readerId)
    {
        // Mark all as read
        FilterDefinition<Message> filter = Builders<Message>.Filter.And(
            Builders<Message>.Filter.Eq(x => x.ConversationId, conversationId),
            Builders<Message>.Filter.Eq(x => x.ReceiverId, readerId),
            Builders<Message>.Filter.Eq(x => x.IsRead, false)
        );

        await _unitOfWork.GetCollection<Message>().UpdateManyAsync(filter, Builders<Message>.Update.Set(x => x.IsRead, true));

        // Update lastMessage.isReadBy
        await _unitOfWork.GetCollection<Conversation>().UpdateOneAsync(
            Builders<Conversation>.Filter.Eq(x => x.Id, conversationId),
            Builders<Conversation>.Update.AddToSet("lastMessage.isReadBy", readerId)
        );

        // Push seen to other client
        // A là người nhận tin nhắn từ B
        // A mở đoạn chat => gọi MarkAsRead
        // Server đánh dấu đã đọc các tin nhắn B gửi cho A
        // Server gửi "MessageSeen" cho B, để B biết A đã đọc
        Conversation otherUser = await _unitOfWork.GetCollection<Conversation>().Find(x => x.Id == conversationId).FirstOrDefaultAsync();
        string? partnerUserId = otherUser.UserIds.FirstOrDefault(u => u != readerId);

        if (partnerUserId != null && OnlineUsers.TryGetValue(partnerUserId, out HashSet<string>? partnerConnections))
        {
            await Clients.Clients(partnerConnections.ToList()).SendAsync("MessageSeen", new
            {
                ConversationId = conversationId,
                SeenBy = readerId
            });
        }
    }

    public async Task DeleteMessage(string messageId, string userId)
    {
        UpdateResult result = await _unitOfWork.GetCollection<Message>().UpdateOneAsync(Builders<Message>.Filter.Eq(m => m.Id, messageId), Builders<Message>.Update.AddToSet(m => m.DeletedForIds, userId));

        if (result.ModifiedCount == 0)
        {
            // Không tìm thấy message hoặc không có quyền xóa
            await Clients.Caller.SendAsync("ReceiveException", "Content not found or you don't have permission to delete this message.");
            return;
        }
        // Gửi cho cả người gửi + người nhận để cập nhật UI
        Message msg = await _unitOfWork.GetCollection<Message>().Find(Builders<Message>.Filter.Eq(m => m.Id, messageId)).FirstOrDefaultAsync();
        if (msg == null)
        {
            return;
        }

        List<string> connectionIds = [];

        if (OnlineUsers.TryGetValue(msg.SenderId, out HashSet<string>? senderConnections))
        {
            connectionIds.AddRange(senderConnections);
        }

        if (OnlineUsers.TryGetValue(msg.ReceiverId, out HashSet<string>? receiverConnections))
        {
            connectionIds.AddRange(receiverConnections);
        }

        await Clients.Clients(connectionIds).SendAsync("MessageDeleted", new
        {
            messageId,
            deletedBy = userId
        });
    }
}

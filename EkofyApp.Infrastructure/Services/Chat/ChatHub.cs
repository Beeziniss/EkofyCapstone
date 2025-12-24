using EkofyApp.Application.Models.Chat;
using EkofyApp.Application.Models.Notifications;
using EkofyApp.Application.Models.Users;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Notifications;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using EkofyApp.Infrastructure.Services.Notifications;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace EkofyApp.Infrastructure.Services.Chat;

public sealed class ChatHub(IUnitOfWork unitOfWork, IHubContext<NotificationHub> notificationHubContext, INotificationService notificationService) : Hub
{
    private static readonly ConcurrentDictionary<string, HashSet<string>> OnlineUsers = []; // userId -> userConnectionId
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHubContext<NotificationHub> _notificationHubContext = notificationHubContext;
    private readonly INotificationService _notificationService = notificationService;

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

        bool wasOffline = false;
        HashSet<string> connections = OnlineUsers.GetOrAdd(userId, _ => []);

        lock (connections)
        {
            wasOffline = connections.Count == 0; // User was offline if no connections existed
            connections.Add(Context.ConnectionId);
        }

        // Nếu user vừa mới online (từ offline -> online), thông báo cho tất cả contacts
        if (wasOffline)
        {
            await NotifyContactsAboutOnlineStatus(userId, true);
        }

        await base.OnConnectedAsync();
    }

    // Ngắt kết nối
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? userId = Context.User?.FindFirst("userId")?.Value;

        if (!string.IsNullOrEmpty(userId) && OnlineUsers.TryGetValue(userId, out HashSet<string>? connections))
        {
            bool goingOffline = false;

            lock (connections)
            {
                connections.Remove(Context.ConnectionId);

                if (connections.Count == 0)
                {
                    OnlineUsers.TryRemove(userId, out _);
                    goingOffline = true; // User is going offline (no more connections)
                }
            }

            // Nếu user vừa offline (từ online -> offline), thông báo cho tất cả contacts
            if (goingOffline)
            {
                await NotifyContactsAboutOnlineStatus(userId, false);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    // THÔNG BÁO TRẠNG THÁI ONLINE/OFFLINE CHO TẤT CẢ CONTACTS
    private async Task NotifyContactsAboutOnlineStatus(string userId, bool isOnline)
    {
        try
        {
            // Lấy tất cả conversations mà user tham gia
            IEnumerable<Conversation> userConversations = await _unitOfWork.GetCollection<Conversation>()
                .Find(c => c.UserIds.Contains(userId))
                .Project<Conversation>(Builders<Conversation>.Projection
                    .Include(x => x.UserIds))
                .ToListAsync();

            // Thu thập tất cả contactIds (những người khác trong conversations)
            HashSet<string> contactIds = [];
            foreach (Conversation conversation in userConversations)
            {
                foreach (string contactId in conversation.UserIds)
                {
                    if (contactId != userId)
                    {
                        contactIds.Add(contactId);
                    }
                }
            }

            // Lấy thông tin user để gửi kèm notification
            NotificationUserInfo? userInfo = await GetUserInfo(userId);
            if (userInfo == null)
            {
                return;
            }

            // Gửi thông báo online/offline status cho tất cả contacts đang online
            foreach (string contactId in contactIds)
            {
                if (OnlineUsers.TryGetValue(contactId, out HashSet<string>? contactConnections))
                {
                    await Clients.Clients(contactConnections.ToList()).SendAsync("ContactStatusChanged", new
                    {
                        UserId = userId,
                        UserInfo = userInfo,
                        IsOnline = isOnline,
                        Timestamp = HelperMethod.GetUtcPlus7TimeOffset()
                    });
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw - this is background notification
            // Logger can be added here if needed
            Log.Logger.Error($"Error notifying contacts about user {userId} status change: {ex.Message}");
        }
    }

    // HELPER METHOD ĐỂ LẤY THÔNG TIN USER
    private async Task<NotificationUserInfo?> GetUserInfo(string userId)
    {
        try
        {
            UserRole userRole = await _unitOfWork.GetCollection<User>()
                .Find(u => u.Id == userId)
                .Project(u => u.Role)
                .FirstOrDefaultAsync();

            if (userRole == UserRole.Listener)
            {
                return await _unitOfWork.GetCollection<Listener>()
                    .Find(l => l.UserId == userId)
                    .Project(l => new NotificationUserInfo
                    {
                        Name = l.DisplayName,
                        Avatar = l.AvatarImage ?? "https://res.cloudinary.com/dofnn7sbx/image/upload/v1730097883/60d5dc467b950c5ccc8ced95_spotify-for-artists_on4me9.jpg"
                    })
                    .FirstOrDefaultAsync();
            }
            else if (userRole == UserRole.Artist)
            {
                return await _unitOfWork.GetCollection<Artist>()
                    .Find(a => a.UserId == userId)
                    .Project(a => new NotificationUserInfo
                    {
                        Name = a.StageName,
                        Avatar = a.AvatarImage ?? "https://res.cloudinary.com/dofnn7sbx/image/upload/v1730097883/60d5dc467b950c5ccc8ced95_spotify-for-artists_on4me9.jpg"
                    })
                    .FirstOrDefaultAsync();
            }

            return null;
        }
        catch
        {
            return null;
        }
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

            dynamic user;
            string role = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
            if (role == UserRole.Listener.ToString())
            {
                user = await _unitOfWork.GetCollection<Listener>()
                    .Find(l => l.UserId == chatMessageRequest.SenderId)
                    .Project(l => new
                    {
                        Name = l.DisplayName,
                        Avatar = l.AvatarImage
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
                        Avatar = a.AvatarImage
                    })
                    .FirstOrDefaultAsync() ?? throw new NotFoundCustomException($"Not found user {chatMessageRequest.SenderId}");
            }

            // Gửi tới tất cả connection của người nhận
            if (OnlineUsers.TryGetValue(chatMessageRequest.ReceiverId, out HashSet<string>? receiverConnections))
            {
                SenderProfile senderProfile = new()
                {
                    Nickname = user.Name,
                    Avatar = user.Avatar,
                };
                await Clients.Clients(receiverConnections.ToList()).SendAsync("ReceiveMessage", message, senderProfile);
            }
            else
            {
                // Người nhận không online, xử lý thông báo push ở đây nếu cần
                await _notificationHubContext.Clients.User(chatMessageRequest.ReceiverId).SendAsync("ReceiveNotification", new NotificationResponse
                {
                    Content = $"You have a new message from {user.Name}.",
                    Avatar = user.Avatar,
                });

                await _unitOfWork.GetCollection<Notification>().InsertOneAsync(new Notification
                {
                    ActorId = chatMessageRequest.SenderId,
                    TargetId = chatMessageRequest.ReceiverId,
                    RelatedId = chatMessageRequest.ConversationId,
                    RelatedType = NotificationRelatedType.Message,
                    Content = $"You have a new message from {user.Name}.",
                    Action = NotificationActionType.Message,
                    Url = $"{Environment.GetEnvironmentVariable("FRONTEND_URL")}/inbox/{chatMessageRequest.ConversationId}",
                });

                Dictionary<string, string> data = [];

                data.Add("mobileRoute", $"/inbox/{chatMessageRequest.ConversationId}");

                await _notificationService.SendFcmNotificationAsync(chatMessageRequest.ReceiverId, "New Message", $"You have a new message from {user.Name}.", "message", data);
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

    // GET ALL ONLINE RECEIVERS ACROSS ALL CONVERSATIONS OF SENDER
    public async Task<IEnumerable<NotificationUserInfo>> GetOnlineReceiversForSender()
    {
        try
        {
            // Lấy senderId từ Context.User
            string? senderId = Context.User?.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(senderId))
            {
                await Clients.Caller.SendAsync("ReceiveException", "Your session has expired. Please login again.");
                return [];
            }

            // Optimized: Only project UserIds to reduce data transfer
            IEnumerable<IEnumerable<string>> receiverIds = await _unitOfWork.GetCollection<Conversation>()
                .Find(c => c.UserIds.Contains(senderId))
                .Project(c => c.UserIds)
                .ToListAsync();

            if (!receiverIds.Any())
            {
                return []; // Sender chưa có conversation nào
            }

            // Optimized: Use LINQ to flatten and filter in one operation
            HashSet<string> allReceiverIds = receiverIds
                .SelectMany(userIds => userIds)
                .Where(userId => userId != senderId)
                .Distinct()
                .ToHashSet(); // Use HashSet for O(1) lookups

            if (allReceiverIds.Count == 0)
            {
                return [];
            }

            // Optimized: Filter online users first before database queries
            IEnumerable<string> onlineReceiverIds = allReceiverIds
                .Where(OnlineUsers.ContainsKey)
                .ToList();

            if (!onlineReceiverIds.Any())
            {
                return [];
            }

            // Optimized: Batch database calls instead of individual queries
            List<NotificationUserInfo> onlineReceiversInfo = [];

            // Get all user roles in one query
            var userRoles = await _unitOfWork.GetCollection<User>()
                .Find(u => onlineReceiverIds.Contains(u.Id))
                .Project(u => new { u.Id, u.Role })
                .ToListAsync();

            Dictionary<string, UserRole> userRolesDict = userRoles.ToDictionary(u => u.Id, u => u.Role);

            // Separate users by role for batch queries
            IEnumerable<string> listenerIds = onlineReceiverIds.Where(id =>
                userRolesDict.TryGetValue(id, out var role) && role == UserRole.Listener).ToList();
            IEnumerable<string> artistIds = onlineReceiverIds.Where(id =>
                userRolesDict.TryGetValue(id, out var role) && role == UserRole.Artist).ToList();

            // Batch query for listeners
            if (listenerIds.Any())
            {
                IEnumerable<NotificationUserInfo> listenerInfos = await _unitOfWork.GetCollection<Listener>()
                    .Find(l => listenerIds.Contains(l.UserId))
                    .Project(l => new NotificationUserInfo
                    {
                        Name = l.DisplayName,
                        Avatar = l.AvatarImage ?? "https://res.cloudinary.com/dofnn7sbx/image/upload/v1730097883/60d5dc467b950c5ccc8ced95_spotify-for-artists_on4me9.jpg"
                    })
                    .ToListAsync();

                onlineReceiversInfo.AddRange(listenerInfos);
            }

            // Batch query for artists
            if (artistIds.Any())
            {
                IEnumerable<NotificationUserInfo> artistInfos = await _unitOfWork.GetCollection<Artist>()
                    .Find(a => artistIds.Contains(a.UserId))
                    .Project(a => new NotificationUserInfo
                    {
                        Name = a.StageName,
                        Avatar = a.AvatarImage ?? "https://res.cloudinary.com/dofnn7sbx/image/upload/v1730097883/60d5dc467b950c5ccc8ced95_spotify-for-artists_on4me9.jpg"
                    })
                    .ToListAsync();

                onlineReceiversInfo.AddRange(artistInfos);
            }

            return onlineReceiversInfo;
        }
        catch (Exception ex)
        {
            // Gửi lỗi về client
            await Clients.Caller.SendAsync("ReceiveException", $"Error getting online receivers: {ex.Message}");
            return [];
        }
    }

    // GET ONLINE RECEIVER IN SPECIFIC CONVERSATION
    public async Task<NotificationUserInfo?> GetOnlineUserInConversation(string conversationId)
    {
        try
        {
            // Lấy senderId từ Context.User
            string? senderId = Context.User?.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(senderId))
            {
                await Clients.Caller.SendAsync("ReceiveException", "Your session has expired. Please login again.");
                return null;
            }

            // Lấy thông tin conversation
            Conversation conversation = await _unitOfWork.GetCollection<Conversation>()
                .Find(c => c.Id == conversationId)
                .FirstOrDefaultAsync();

            if (conversation == null)
            {
                await Clients.Caller.SendAsync("ReceiveException", "Conversation not found.");
                return null;
            }

            // Kiểm tra sender có trong conversation này không
            if (!conversation.UserIds.Contains(senderId))
            {
                await Clients.Caller.SendAsync("ReceiveException", "You are not a participant in this conversation.");
                return null;
            }

            // Lấy receiverId (user còn lại trong conversation)
            string? receiverId = conversation.UserIds.FirstOrDefault(userId => userId != senderId);

            if (string.IsNullOrEmpty(receiverId))
            {
                return null;
            }

            // Kiểm tra receiver có online không
            if (!OnlineUsers.ContainsKey(receiverId))
            {
                return null; // Receiver không online
            }

            // Lấy thông tin receiver
            return await GetUserInfo(receiverId);
        }
        catch (Exception ex)
        {
            // Gửi lỗi về client
            await Clients.Caller.SendAsync("ReceiveException", $"Error getting online receiver: {ex.Message}");
            return null;
        }
    }
}

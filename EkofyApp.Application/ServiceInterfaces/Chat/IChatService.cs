using EkofyApp.Application.Models.Conversations;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.ServiceInterfaces.Chat;
public interface IChatService
{
    Task AddConversationFromRequestHubAsync(CreateConversationRequest request);
    IQueryable<Conversation> GetConversations();
    IQueryable<Conversation> GetConversationsByUserId(string userId);
    IQueryable<Message> GetMessages();
    Task UpdateConversationStatusAsync(string conversationId, ConversationStatus status);
}

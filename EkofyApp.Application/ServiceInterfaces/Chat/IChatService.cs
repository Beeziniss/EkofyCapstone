using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.ServiceInterfaces.Chat;
public interface IChatService
{
    IQueryable<Conversation> GetConversations();
    IQueryable<Conversation> GetConversationsByUserId(string userId);
    IQueryable<Message> GetMessages();
    Task UpdateConversationStatusAsync(string conversationId, ConversationStatus status);
}

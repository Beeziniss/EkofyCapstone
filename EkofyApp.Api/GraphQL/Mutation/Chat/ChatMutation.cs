using EkofyApp.Application.Models.Conversations;
using EkofyApp.Application.ServiceInterfaces.Chat;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Api.GraphQL.Mutation.Chat;

[ExtendObjectType<MutationInitialization>]
[MutationType]
public sealed class ChatMutation(IChatService chatService)
{
    private readonly IChatService _chatService = chatService;

    public async Task<bool> UpdateConversationStatusAsync(string conversationId, ConversationStatus status)
    {
        await _chatService.UpdateConversationStatusAsync(conversationId, status);
        return true;
    }

    public async Task<string> AddConversationGeneralAsync(string otherUserId)
    {
        return await _chatService.AddConversationGeneralAsync(otherUserId);
    }

    public async Task<string> AddConversationFromRequestHubAsync(CreateConversationRequest request)
    {
        return await _chatService.AddConversationFromRequestHubAsync(request);
    }
}

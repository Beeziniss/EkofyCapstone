using EkofyApp.Application.ServiceInterfaces.Chat;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Chats;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public class ChatQuery(IChatService chatGraphQLService)
{
    private readonly IChatService _chatGraphQLService = chatGraphQLService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UsePaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Message>]
    public IQueryable<Message> GetMessages()
    {
        return _chatGraphQLService.GetMessages();
    }

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Conversation>]
    public IQueryable<Conversation> GetConversations()
    {
        return _chatGraphQLService.GetConversations();
    }

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Conversation>]
    public IQueryable<Conversation> GetConversationsByUserId(string userId)
    {
        return _chatGraphQLService.GetConversationsByUserId(userId);
    }
}

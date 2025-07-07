using EkofyApp.Application.ServiceInterfaces.Chat;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.Chats;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public class ChatQuery(IChatGraphQLService chatGraphQLService)
{
    private readonly IChatGraphQLService _chatGraphQLService = chatGraphQLService;

    public IQueryable<Message> GetMessagesExecutable()
    {
        return _chatGraphQLService.GetMessagesExecutable();
    }
}

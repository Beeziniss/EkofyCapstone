using EkofyApp.Application.Models.Chat;

namespace EkofyApp.Api.GraphQL.Query.Chats;

public class ChatQueryExtension : ObjectTypeExtension<ChatQuery>
{
    protected override void Configure(IObjectTypeDescriptor<ChatQuery> descriptor)
    {
        descriptor.Field(x => x.GetMessagesExecutable())
            .UseProjection<MessageResponse>();
    }
}

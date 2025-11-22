using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Chat;

public sealed class ChatMutationExtension : ObjectTypeExtension<ChatMutation>
{
    protected override void Configure(IObjectTypeDescriptor<ChatMutation> descriptor)
    {
        descriptor.Field(x => x.UpdateConversationStatusAsync(default!, default!))
            .Authorize(HelperRoleBase.ListenerArtistModeratorRolesArray);

        descriptor.Field(x => x.AddConversationGeneralAsync(default!))
            .Authorize(HelperRoleBase.ListenerArtistRolesArray);

        descriptor.Field(x => x.AddConversationFromRequestHubAsync(default!))
            .Authorize(HelperRoleBase.ListenerArtistRolesArray);
    }
}

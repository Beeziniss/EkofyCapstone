using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Listeners;

public sealed class ListenerMutationExtension : ObjectTypeExtension<ListenerMutation>
{
    protected override void Configure(IObjectTypeDescriptor<ListenerMutation> descriptor)
    {
        descriptor.Field(f => f.UpdateListenerProfileAsync(default!))
            .Authorize(HelperRoleBase.ListenerRolesArray);
    }
}

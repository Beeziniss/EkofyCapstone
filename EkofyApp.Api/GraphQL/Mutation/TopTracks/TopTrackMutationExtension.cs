using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.TopTracks
{
    public class TopTrackMutationExtension : ObjectTypeExtension<TopTrackMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<TopTrackMutation> descriptor)
        {
            // Configure the TopTrackMutation type here if needed
            descriptor.Field(x => x.UpsertTopTrackCountAsync(default!))
                .Authorize(roles: HelperRoleBase.ListenerRolesArray);
        }
    }
}

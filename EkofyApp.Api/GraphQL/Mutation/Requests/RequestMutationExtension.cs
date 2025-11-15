using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Requests
{
    public class RequestMutationExtension : ObjectTypeExtension<RequestMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<RequestMutation> descriptor)
        {
            descriptor.Field(f => f.SendRequest(default!, default))
                .Authorize(HelperRoleBase.ListenerRolesArray);
            descriptor.Field(f => f.ChangeRequestStatusAsync(default!))
                .Authorize(HelperRoleBase.ListenerArtistRolesArray);
            descriptor.Field(f => f.CreatePublicRequestAsync(default!))
                .Authorize(HelperRoleBase.ListenerRolesArray);
            descriptor.Field(x => x.UpdatePublicRequestAsync(default!))
                .Authorize(HelperRoleBase.ListenerRolesArray);
            descriptor.Field(x => x.BlockPublicRequestAsync(default!))
                .Authorize(HelperRoleBase.ModeratorRolesArray);
        }
    }
}

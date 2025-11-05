using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.RequestHubs
{
    public class RequestMutationExtension : ObjectTypeExtension<RequestMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<RequestMutation> descriptor)
        {
            descriptor.Field(f => f.CreateRequestAsync(default!))
                .Authorize(HelperRoleBase.ListenerRolesArray);
            descriptor.Field(x => x.UpdateRequestAsync(default!))
                .Authorize(HelperRoleBase.ListenerRolesArray);
            descriptor.Field(x => x.BlockRequestAsync(default!))
                .Authorize(HelperRoleBase.ModeratorRolesArray);
        }
    }
}

using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Subscriptions;

public sealed class SubscriptionMutationExtension : ObjectTypeExtension<SubscriptionMutation>
{
    protected override void Configure(IObjectTypeDescriptor<SubscriptionMutation> descriptor)
    {
        descriptor.Field(x => x.CreateSubscriptionAsync(default!))
            .Authorize(HelperRoleBase.AdminRolesArray);

        descriptor.Field(x => x.CreateSubscriptionPlanAsync(default!))
            .Authorize(HelperRoleBase.AdminRolesArray);

        descriptor.Field(x => x.UpdateSubscriptionPlanAsync(default!))
            .Authorize(HelperRoleBase.AdminRolesArray);

        descriptor.Field(x => x.DeprecateSubscriptionAsync(default!))
            .Authorize(HelperRoleBase.AdminRolesArray);

        //descriptor.Field(x => x.UpdateEntitlementsSubscriptionAsync(default!))
        //    .Authorize(roles: "Admin");
    }
}

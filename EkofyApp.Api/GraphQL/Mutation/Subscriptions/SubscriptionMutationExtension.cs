namespace EkofyApp.Api.GraphQL.Mutation.Subscriptions;

public sealed class SubscriptionMutationExtension : ObjectTypeExtension<SubscriptionMutation>
{
    protected override void Configure(IObjectTypeDescriptor<SubscriptionMutation> descriptor)
    {
        descriptor.Field(x => x.CreateSubscriptionAsync(default!))
            //.Authorize(roles: "Admin");
            .AllowAnonymous();

        descriptor.Field(x => x.CreateSubscriptionPlanAsync(default!))
            .Authorize(roles: "Admin");

        descriptor.Field(x => x.DeprecateSubscriptionAsync(default!))
            .Authorize(roles: "Admin");

        //descriptor.Field(x => x.UpdateEntitlementsSubscriptionAsync(default!))
        //    .Authorize(roles: "Admin");
    }
}

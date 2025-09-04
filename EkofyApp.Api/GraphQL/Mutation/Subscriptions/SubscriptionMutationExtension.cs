namespace EkofyApp.Api.GraphQL.Mutation.Subscriptions;

public sealed class SubscriptionMutationExtension : ObjectTypeExtension<SubscriptionMutation>
{
    protected override void Configure(IObjectTypeDescriptor<SubscriptionMutation> descriptor)
    {
        descriptor.Field(x => x.CreateSubscriptionAsync(default!))
            .Authorize(roles: "Admin");
    }
}

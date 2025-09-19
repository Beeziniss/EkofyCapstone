namespace EkofyApp.Api.GraphQL.Query.Subscriptions;

public sealed class SubscriptionPlanQueryExtension : ObjectTypeExtension<SubscriptionPlanQuery>
{
    protected override void Configure(IObjectTypeDescriptor<SubscriptionPlanQuery> descriptor)
    {
        descriptor.Field(x => x.GetSubscriptionPlans())
            .Authorize(roles: ["Listener", "Artist", "Moderator", "Admin"]);
        //.AllowAnonymous();
    }
}

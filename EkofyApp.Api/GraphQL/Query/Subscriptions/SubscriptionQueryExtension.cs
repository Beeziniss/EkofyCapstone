namespace EkofyApp.Api.GraphQL.Query.Subscriptions;

public sealed class SubscriptionQueryExtension : ObjectTypeExtension<SubscriptionQuery>
{
    protected override void Configure(IObjectTypeDescriptor<SubscriptionQuery> descriptor)
    {
        descriptor.Field(x => x.GetSubscriptions())
            .Authorize(roles: ["Listener", "Artist", "Moderator", "Admin"]);
        //.AllowAnonymous();
    }
}

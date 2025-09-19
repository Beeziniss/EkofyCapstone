namespace EkofyApp.Api.GraphQL.Query.UserSubscriptions;

public sealed class UserSubscriptionQueryExtension : ObjectTypeExtension<UserSubscriptionQuery>
{
    protected override void Configure(IObjectTypeDescriptor<UserSubscriptionQuery> descriptor)
    {
        descriptor.Field(x => x.GetUserSubscriptions())
            .Authorize(roles: ["Listener", "Artist", "Moderator", "Admin"]);
        //.AllowAnonymous();
    }
}

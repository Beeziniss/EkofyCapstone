using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.Subscriptions;

public sealed class SubscriptionQueryExtension : ObjectTypeExtension<SubscriptionQuery>
{
    protected override void Configure(IObjectTypeDescriptor<SubscriptionQuery> descriptor)
    {
        descriptor.Field(x => x.GetSubscriptions())
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting<Subscription>();
        //.AllowAnonymous();
    }
}

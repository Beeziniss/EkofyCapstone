using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.UserSubscriptions;

public sealed class UserSubscriptionQueryExtension : ObjectTypeExtension<UserSubscriptionQuery>
{
    protected override void Configure(IObjectTypeDescriptor<UserSubscriptionQuery> descriptor)
    {
        descriptor.Field(x => x.GetUserSubscriptions())
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting<UserSubscription>();
        //.AllowAnonymous();
    }
}

using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.Subscriptions;

public sealed class SubscriptionPlanQueryExtension : ObjectTypeExtension<SubscriptionPlanQuery>
{
    protected override void Configure(IObjectTypeDescriptor<SubscriptionPlanQuery> descriptor)
    {
        descriptor.Field(x => x.GetSubscriptionPlans())
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting<SubscriptionPlan>();
        //.AllowAnonymous();
    }
}
